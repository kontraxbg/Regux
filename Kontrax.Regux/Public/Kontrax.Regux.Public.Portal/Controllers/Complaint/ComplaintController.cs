using BotDetect.Web.Mvc;
using Kontrax.Regux.Data;
using Kontrax.Regux.Public.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Public.Portal.Controllers.Complaint
{
    // Засега, докато реализираме вход за потребители
    [AllowAnonymous]
    public class ComplaintController : Controller
    {
        protected readonly ReguxEntities _context = new ReguxEntities();

        public ComplaintController()
        {
        }

        public ActionResult Create()
        {
            ComplaintModel model = new ComplaintModel();
            LoadListsForModel(model);

            return View(model);
        }

        [HttpPost]
        [CaptchaValidation("CaptchaCode", "ExampleCaptcha", "Неверен CAPTCHA код!")]
        public async Task<ActionResult> Create(ComplaintModel model)
        {
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
                //model.ServiceName = _context.Services.Find(model.ServiceId).Name;
                //model.AdministrationName = _context.Administrations.Find(model.AdministrationId).Name;
                model.EmployeeName = _context.AspNetUsers.Find(model.EmployeeId).PersonName;
                model.SubmitDateTime = DateTime.Now;
                model.IncidentDateTime = model.IncidentDateTime;
            }

            try
            {
                Signal signal = new Signal();
                signal.SubmitDateTime = model.SubmitDateTime;
                signal.IncidentDateTime = model.IncidentDateTime;
                signal.ServiceId = model.ServiceId;
                signal.AdministrationId = model.AdministrationId;
                signal.EmployeeId = model.EmployeeId;
                signal.EmployeeName = model.EmployeeName;
                signal.SenderNote = model.SenderNote;

                if (!model.IsAnonymous)
                {
                    signal.SenderPidTypeCode = model.SenderPidTypeCode;
                    signal.SenderIdentifier = model.SenderIdentifier;
                    signal.SenderContact = model.SenderContact;
                }

                _context.Signals.Add(signal);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", "Complaint");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Unexpected error", ex.Message);
                return View(model);
            }
        }

        public ActionResult Details()
        {
            var results = _context.Signals.OrderByDescending(x => x.SubmitDateTime).ToList();

            List<ComplaintViewModel> model = new List<ComplaintViewModel>();
            foreach (var item in results)
            {
                model.Add(new ComplaintViewModel()
                {
                    Id = item.Id,
                    AdministrationName = item.Administration.Name,
                    EmployeeName = item.EmployeeName,
                    IncidentDateTime = item.IncidentDateTime,
                    SubmitDateTime = item.SubmitDateTime,
                    SenderContact = item.SenderContact,
                    SenderIdentifier = item.SenderIdentifier,
                    SenderNote = item.SenderNote,
                    SenderPidTypeCode = item.SenderPidTypeCode,
                    ServiceName = item.Service.Name
                });
            }

            return View(model);
        }

        private void LoadListsForModel(ComplaintModel model)
        {
            model.Services = new IdNameSelectList(from x in _context.Services.ToList()
                                                  select new IdNameModel(x.Id, x.Name));

            model.Administrations = new IdNameSelectList(from x in _context.Administrations.ToList()
                                                         select new IdNameModel(x.Id, x.Name));

            model.Employees = new CodeNameSelectList(from x in _context.AspNetUsers.ToList()
                                                     select new CodeNameModel(x.Id, x.PersonName));

            model.PidTypes = new CodeNameSelectList(from x in _context.PidTypes.ToList()
                                                    select new CodeNameModel(x.Code, x.Name));
        }
    }
}