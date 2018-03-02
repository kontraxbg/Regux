using BotDetect.Web.Mvc;
using Kontrax.Regux.Data;
using Kontrax.Regux.Public.Portal.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Kontrax.Regux.Public.Portal.Controllers.Complaint
{
    [Authorize]
    public class ComplaintController : BaseController
    {
        public ActionResult Create()
        {
            DateTime now = DateTime.Now;
            ComplaintModel model = new ComplaintModel
            {
                SubmitDateTime = now,
                IncidentDateTime = now
            };
            LoadListsForModel(model);
            return View(model);
        }

        [HttpPost]
        [CaptchaValidation("CaptchaCode", "ExampleCaptcha", "Неверен CAPTCHA код!")]
        public async Task<ActionResult> Create(ComplaintModel model)
        {
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (!ModelState.IsValid)
            {
                // TODO: Captcha validation failed, show error message      
                LoadListsForModel(model);
                ModelState.AddModelError("CaptchaError", "CAPTCHA тестът бе неуспешен. Моля опитайте отново.");
                return View(model);
            }
            else
            {
                // TODO: Captcha validation passed, proceed with protected action  
                MvcCaptcha.ResetCaptcha("ExampleCaptcha");
                if (model.EmployeeId != null)
                {
                    model.EmployeeName = _context.AspNetUsers.Where(x => x.Id == model.EmployeeId).FirstOrDefault()?.PersonName;
                }
            }

            try
            {
                Signal signal = new Signal
                {
                    SenderId = user.Id,
                    SubmitDateTime = DateTime.Now,
                    IncidentDateTime = model.IncidentDateTime,
                    ServiceId = model.ServiceId,
                    AdministrationId = model.AdministrationId,
                    EmployeeId = model.EmployeeId,
                    EmployeeName = model.EmployeeName,
                    SenderNote = model.SenderNote,
                    SenderContact = model.SenderContact,
                    IsProposal = false
                };

                _context.Signals.Add(signal);
                await _context.SaveChangesWithValidationExplainedAsync();

                return RedirectToAction(nameof(Details), "Complaint");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Unexpected error", ex.Message);
                return View(model);
            }
        }

        public ActionResult Details()
        {
            string userId = User.Identity.GetUserId();

            List<SignalViewModel> model = (
                from s in _context.Signals
                orderby s.SubmitDateTime descending
                where s.SenderId == userId
                select s).ToList().Select(x => new SignalViewModel(x)).ToList();

            return View(model);
        }

        public JsonResult GetServicesForAdministration(string id)
        {
            var result = (
                from s in _context.Activities
                where s.AdministrationId.ToString() == id
                select new JsonTestModel()
                {
                    Id = s.ProvidedServiceId.ToString(),
                    Name = s.Service.Name,
                    IsSpecial = ((s.SearchAdmServiceMainData || s.SearchBatchesForAdmService) ? 0 : 1).ToString()
                }).ToArray();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private void LoadListsForModel(ComplaintModel model)
        {
            //model.Services = new IdNameSelectList(from x in _context.Services.ToList()
            //                                      select new IdNameModel(x.Id, x.Name));

            model.Administrations = new IdNameSelectList(from x in _context.Administrations.ToList()
                                                         select new IdNameModel(x.Id, x.Name));

            //model.Employees = new CodeNameSelectList(from x in _context.AspNetUsers.ToList()
            //                                         select new CodeNameModel(x.Id, x.PersonName));
        }
    }
}