using AgentOrchestration.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AgentOrchestration.Services
{
    /// <summary>
    /// Service for persisting campaign session context to simulate long-running conversations
    /// </summary>
    public class ContextPersistenceService
    {
        private readonly string _storageDirectory;
        private readonly string _activeSessionsFile;

        public ContextPersistenceService(string storageDirectory = "campaign_sessions")
        {
            _storageDirectory = storageDirectory;
            _activeSessionsFile = Path.Combine(_storageDirectory, "active_sessions.json");
            
            // Ensure storage directory exists
            Directory.CreateDirectory(_storageDirectory);
        }

        /// <summary>
        /// Saves campaign session state to persistent storage
        /// </summary>
        public async Task SaveSessionAsync(CampaignSession session)
        {
            try
            {
                session.LastUpdated = DateTime.UtcNow;
                
                var sessionFile = Path.Combine(_storageDirectory, $"session_{session.Id}.json");
                var json = JsonConvert.SerializeObject(session, Formatting.Indented);
                await File.WriteAllTextAsync(sessionFile, json);

                // Update active sessions index
                await UpdateActiveSessionsIndex(session);

                Console.WriteLine($"Session saved: {session.Id} at {session.LastUpdated:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving session {session.Id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads campaign session state from persistent storage
        /// </summary>
        public async Task<CampaignSession?> LoadSessionAsync(string sessionId)
        {
            try
            {
                var sessionFile = Path.Combine(_storageDirectory, $"session_{sessionId}.json");
                
                if (!File.Exists(sessionFile))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(sessionFile);
                var session = JsonConvert.DeserializeObject<CampaignSession>(json);
                
                Console.WriteLine($"Session loaded: {sessionId} (last updated: {session?.LastUpdated:yyyy-MM-dd HH:mm:ss})");
                return session;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session {sessionId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all active campaign sessions
        /// </summary>
        public async Task<List<CampaignSession>> GetActiveSessionsAsync()
        {
            try
            {
                var sessions = new List<CampaignSession>();
                var sessionIds = await GetActiveSessionIds();

                foreach (var sessionId in sessionIds)
                {
                    var session = await LoadSessionAsync(sessionId);
                    if (session != null && session.IsActive)
                    {
                        sessions.Add(session);
                    }
                }

                return sessions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting active sessions: {ex.Message}");
                return new List<CampaignSession>();
            }
        }

        /// <summary>
        /// Resumes a campaign session from persistent storage
        /// </summary>
        public async Task<(bool success, CampaignSession? session, string message)> ResumeSessionAsync(string sessionId)
        {
            try
            {
                var session = await LoadSessionAsync(sessionId);
                
                if (session == null)
                {
                    return (false, null, $"Session {sessionId} not found");
                }

                if (!session.IsActive)
                {
                    return (false, null, $"Session {sessionId} is not active");
                }

                var timeSinceLastUpdate = DateTime.UtcNow - session.LastUpdated;
                var resumeMessage = $"Session resumed after {timeSinceLastUpdate.TotalHours:F1} hours";
                
                // Add resume event to execution log
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: {resumeMessage}");

                return (true, session, resumeMessage);
            }
            catch (Exception ex)
            {
                return (false, null, $"Error resuming session: {ex.Message}");
            }
        }

        /// <summary>
        /// Marks a session as inactive
        /// </summary>
        public async Task DeactivateSessionAsync(string sessionId)
        {
            try
            {
                var session = await LoadSessionAsync(sessionId);
                if (session != null)
                {
                    session.IsActive = false;
                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Session deactivated");
                    await SaveSessionAsync(session);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deactivating session {sessionId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up old inactive sessions
        /// </summary>
        public async Task CleanupOldSessionsAsync(int maxAgeHours = 24)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-maxAgeHours);
                var sessionFiles = Directory.GetFiles(_storageDirectory, "session_*.json");

                foreach (var sessionFile in sessionFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(sessionFile);
                        var session = JsonConvert.DeserializeObject<CampaignSession>(json);
                        
                        if (session != null && session.LastUpdated < cutoffTime && !session.IsActive)
                        {
                            File.Delete(sessionFile);
                            Console.WriteLine($"Cleaned up old session: {session.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing session file {sessionFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a summary of all sessions
        /// </summary>
        public async Task<string> GetSessionsSummaryAsync()
        {
            try
            {
                var sessions = await GetActiveSessionsAsync();
                var summary = $@"
Campaign Sessions Summary:
- Total Active Sessions: {sessions.Count}
- Sessions by Status:
{string.Join("\n", sessions.GroupBy(s => s.Campaign.Status).Select(g => $"  • {g.Key}: {g.Count()}"))}

Recent Sessions:
{string.Join("\n", sessions.OrderByDescending(s => s.LastUpdated).Take(5).Select(s => 
    $"  • {s.Id}: {s.Campaign.Goal} ({s.Campaign.Status}) - {s.LastUpdated:yyyy-MM-dd HH:mm:ss}"))}

Storage Location: {_storageDirectory}
";
                return summary;
            }
            catch (Exception ex)
            {
                return $"Error generating summary: {ex.Message}";
            }
        }

        private async Task UpdateActiveSessionsIndex(CampaignSession session)
        {
            try
            {
                var activeSessionIds = await GetActiveSessionIds();
                
                if (session.IsActive && !activeSessionIds.Contains(session.Id))
                {
                    activeSessionIds.Add(session.Id);
                }
                else if (!session.IsActive && activeSessionIds.Contains(session.Id))
                {
                    activeSessionIds.Remove(session.Id);
                }

                var json = JsonConvert.SerializeObject(activeSessionIds, Formatting.Indented);
                await File.WriteAllTextAsync(_activeSessionsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating active sessions index: {ex.Message}");
            }
        }

        private async Task<List<string>> GetActiveSessionIds()
        {
            try
            {
                if (!File.Exists(_activeSessionsFile))
                {
                    return new List<string>();
                }

                var json = await File.ReadAllTextAsync(_activeSessionsFile);
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading active sessions index: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
