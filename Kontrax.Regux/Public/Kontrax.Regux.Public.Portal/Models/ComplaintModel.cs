using System;
using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Public.Portal.Models
{
    public class ComplaintModel
    {
        [Display(Name = "Подаден сигнал")]
        [DataType(DataType.DateTime)]
        public DateTime SubmitDateTime { get; set; }

        [Display(Name = "Дата събитие")]
        [DataType(DataType.DateTime)]
        public DateTime IncidentDateTime { get; set; }

        [Required(ErrorMessage = "Полето е задължително.")]
        [Display(Name = "Услуга")]
        public int ServiceId { get; set; }

        // Избор на услуга (от списъка с дефинирани услуги)
        public string ServiceName { get; set; }

        [Required(ErrorMessage = "Полето е задължително.")]
        [Display(Name = "Администрация")]
        public int AdministrationId { get; set; }

        // Избор на администрация (от списъка с администрации, за които има регистриран локален администратор)
        public string AdministrationName { get; set; }

        public IdNameSelectList Services { get; set; }

        // Избор на служител(от служителите за съответната администрация);
        public string EmployeeName { get; set; }

        [Display(Name = "Служител")]
        public string EmployeeId { get; set; }

        public CodeNameSelectList Employees { get; set; }

        public IdNameSelectList Administrations { get; set; }

        [Display(Name = "Данни за контакт")]
        public string SenderContact { get; set; }

        [Display(Name = "Описание")]
        public string SenderNote { get; set; }
    }
}