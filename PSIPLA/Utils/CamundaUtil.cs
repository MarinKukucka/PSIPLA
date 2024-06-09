using Camunda.Api.Client;
using Camunda.Api.Client.History;
using Camunda.Api.Client.Message;
using Camunda.Api.Client.ProcessDefinition;
using Camunda.Api.Client.User;
using Camunda.Api.Client.UserTask;
using PSIPLA.Models;

namespace PSIPLA.Utils
{
    public class CamundaUtil
    {
        private const string camundaEngineUri = "http://localhost:8080/engine-rest";
        private static CamundaClient client = CamundaClient.Create(camundaEngineUri);
        private const string processKey = "SessionReviewProcess";
        private const string applyMessage = "SessionAccepted";

        public static async Task<string> StartSessionProcess(int sessionId, string patient)
        {
            var parameters = new Dictionary<string, object> 
            {
                { "SessionId", sessionId },
                { "Patient", patient }
            };
            var processInstanceId = await StartProcess(parameters);
            return processInstanceId;
        }

        private static async Task<string> StartProcess(Dictionary<string, object> processParameters)
        {
            var client = CamundaClient.Create(camundaEngineUri);
            StartProcessInstance parameters = new StartProcessInstance();
            foreach(var param in processParameters)
            {
                parameters.SetVariable(param.Key, param.Value);
            }
            var definition = client.ProcessDefinitions.ByKey(processKey);
            var processInstance = await definition.StartProcessInstance(parameters);
            return processInstance.Id;
        }

        public static async Task ApplyForSession(string pid, string psychologist)
        {
            var message = new CorrelationMessage
            {
                ProcessInstanceId = pid,
                MessageName = applyMessage,
                All = true,
                BusinessKey = null
            };
            message.ProcessVariables.Set("Psychologist", psychologist);
            await client.Messages.DeliverMessage(message);
        }

        public static async Task<bool> IsUserInGroup(string user, string group)
        {
            var list = await client.Users
                                   .Query(new UserQuery
                                   {
                                       Id = user,
                                       MemberOfGroup = group
                                   })
                                   .List();
            return list.Count > 0;
        }

        public static async Task ConductSession(string taskId, string note)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["Note"] = VariableValue.FromObject(note);
            await client.UserTasks[taskId].Complete(new CompleteTask()
            {
                Variables = variables
            });
        }

        public static async Task<List<SessionInfo>> GetSessions()
        {
            var historyList = await client.History.ProcessInstances.Query(new HistoricProcessInstanceQuery { ProcessDefinitionKey = processKey }).List();
            var sessions = historyList.OrderBy(p => p.StartTime)
                                      .Select(p => new SessionInfo
                                      {
                                          RequestDate = p.StartTime,
                                          EndTime = p.State == ProcessInstanceState.Completed ? p.EndTime : null,
                                          IsCompleted = p.State == ProcessInstanceState.Completed,
                                          PID = p.Id
                                      })                  
                                      .ToList();
            var tasks = sessions.Select(LoadSessionVariables);
            await Task.WhenAll(tasks);
            return sessions;
        }

        public static async Task<List<TaskInfo>> GetTasks(string username)
        {
            var userTasks = await client.UserTasks
                                        .Query(new TaskQuery
                                        {
                                            Assignee = username,
                                            ProcessDefinitionKey = processKey
                                        })
                                        .List();

            var list = userTasks.OrderBy(t => t.Created)
                                .Select(t => new TaskInfo
                                {
                                    TID = t.Id,
                                    TaskName = t.Name,
                                    TaskKey = t.TaskDefinitionKey,
                                    PID = t.ProcessInstanceId,
                                    RequestDate = t.Created
                                })
                                .ToList();

            var tasks = new List<Task>();
            foreach(var task in list)
            {
                tasks.Add(LoadTaskVariables(task));
            }
            await Task.WhenAll(tasks);
            return list;
        }

        private static async Task LoadTaskVariables(TaskInfo task)
        {
            var variables = await client.UserTasks[task.TID].Variables.GetAll();

            if(variables.TryGetValue("SessionId", out VariableValue value))
            {
                task.SessionId = value.GetValue<int>();
            }

            if(variables.TryGetValue("Patient", out value))
            {
                task.Patient = value.GetValue<string>();
            }

            if(variables.TryGetValue("Note", out value))
            {
                task.note = value.GetValue<string>();
            }
        }

        private static async Task LoadSessionVariables(SessionInfo session)
        {
            var list = await client.History.VariableInstances.Query(new HistoricVariableInstanceQuery { ProcessInstanceId = session.PID }).List();
            session.SessionId = list.Where(v => v.Name == "SessionId")
                                    .Select(v => Convert.ToInt32(v.Value))
                                    .First();

            session.Patient = list.Where(v => v.Name == "Patient")
                                  .Select(v => (string)v.Value)
                                  .First();

            var psychologist = list.Where(v => v.Name == "Psychologist")
                                   .Select(v => v.Value as string)
                                   .FirstOrDefault();
            session.Psychologist = psychologist;

            session.Note = list.Where(v => v.Name == "Note")
                               .Select(v => v.Value as string)
                               .FirstOrDefault();

            var timePassed = list.Where(v => v.Name == "TimePassed")
                                 .Select(v => v.Value)
                                 .FirstOrDefault();

            session.CanAcceptSession = string.IsNullOrEmpty(psychologist) && (timePassed == null || !Convert.ToBoolean(timePassed));
        }
    }
}
