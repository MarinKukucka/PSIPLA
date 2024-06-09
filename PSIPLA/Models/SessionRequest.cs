namespace PSIPLA.Models
{
    public class SessionInfo
    {
        public int SessionId { get; set; }
        public string Patient { get; set; }
        public string Psychologist { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public bool CanAcceptSession { get; set; }
        public string PID { get; set; }
        public string Note { get; set; }
    }

    public class DashboardData
    {
        public List<SessionInfo> ProcessInstances { get; set; }
        public List<SessionInfo> AllSessions { get; set; }
        public List<SessionInfo> AvailableSessions { get; set; }
        public List<SessionInfo> ConductedSessions { get; set; }
        public List<TaskInfo> MyTasks { get; set; }
    }

    public class TaskInfo
    {
        public string TID { get; set; }
        public string TaskKey { get; set; }
        public string TaskName { get; set; }
        public string Patient { get; set; }
        public int SessionId { get; set; }
        public string PID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Psychologist { get; set; }
        public string note { get; set; }
    }
}
