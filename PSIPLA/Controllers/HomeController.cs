using Microsoft.AspNetCore.Mvc;
using PSIPLA.Models;
using PSIPLA.Utils;

namespace PSIPLA.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index(string user)
        {
            var data = new DashboardData();
            data.MyTasks = await CamundaUtil.GetTasks(user);
            data.AllSessions = await CamundaUtil.GetSessions();
            var allSessions = await CamundaUtil.GetSessions();
            if (await CamundaUtil.IsUserInGroup(user, "Psychologists"))
            {
                data.AvailableSessions = allSessions.Where(session => !session.IsCompleted).ToList();
                data.ConductedSessions = allSessions.Where(session => session.Psychologist == user && session.IsCompleted).ToList();
            }
            else if(await CamundaUtil.IsUserInGroup(user, "Pacients"))
            {
                data.AvailableSessions = allSessions.Where(session => session.Patient == user && !session.IsCompleted).ToList();
                data.ConductedSessions = allSessions.Where(session => session.Patient == user && session.IsCompleted && session.Psychologist != null).ToList();
            }
            return View(data);
        }

        [HttpGet]
        public IActionResult RequestSession()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestSession(string user, int id)
        {
            if (await CamundaUtil.IsUserInGroup(user, "Pacients"))
            {
                var pid = await CamundaUtil.StartSessionProcess(id, user);
            }
            else 
            {
                TempData["Error"] = "Only patients can request a sessions.";
            }
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> ApplyForSession(string user, string pid)
        {
            if(await CamundaUtil.IsUserInGroup(user, "Psychologists"))
            {
                await CamundaUtil.ApplyForSession(pid, user);
            }
            else
            {
                TempData["Error"] = "Only psychologists can apply for sessions.";
            }
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> ConductSession(string user, string taskId, string note)
        {
            await CamundaUtil.ConductSession(taskId, note);
            return RedirectToAction(nameof(Index), new { user });
        }
    }
}
