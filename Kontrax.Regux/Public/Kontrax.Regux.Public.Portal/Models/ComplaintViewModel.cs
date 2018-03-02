using System;
using System.ComponentModel.DataAnnotations;
using Kontrax.Regux.Data;

namespace Kontrax.Regux.Public.Portal.Models
{
    public class SignalViewModel
    {
        public SignalViewModel()
        {
        }

        public SignalViewModel(Signal signal)
        {
            Id = signal.Id;
            AdministrationName = signal.Administration.Name;
            EmployeeName = signal.EmployeeName;
            IncidentDateTime = signal.IncidentDateTime;
            SubmitDateTime = signal.SubmitDateTime;
            SenderContact = signal.SenderContact;
            SenderNote = signal.SenderNote;
            ServiceName = signal.Service.Name;
            IsProposal = signal.IsProposal;
        }

        public int Id { get; set; }

        public bool IsProposal { get; set; }

        [Display(Name = "Вид")]
        public string ServiceType
        {
            get { return IsProposal ? "Предложение" : "Сигнал"; }
        }

        [Display(Name = "Услуга")]
        public string ServiceName { get; set; }

        [Display(Name = "Администрация")]
        public string AdministrationName { get; set; }

        [Display(Name = "Служител")]
        public string EmployeeName { get; set; }

        [Display(Name = "Дата събитие")]
        [DataType(DataType.DateTime)]
        //[DisplayFormat(ApplyFormatInEditMode = false, DataFormatString = "{0:dd.MM.yyyy HH:mm г.}")]
        public DateTime? IncidentDateTime { get; set; }
        public string IncidentDateTimeString
        {
            get
            {
                return string.Format("{0:dd.MM.yyyy г. HH:mm }", IncidentDateTime);
            }
        }

        [Display(Name = "Подаден сигнал")]
        [DataType(DataType.DateTime)]
        //[DisplayFormat(ApplyFormatInEditMode = false, DataFormatString = "{0:dd.MM.yyyy HH:mm г.}")]
        public DateTime SubmitDateTime { get; set; }

        public string SubmitDateTimeString
        {
            get
            {
                return string.Format("{0:dd.MM.yyyy г. HH:mm }", SubmitDateTime);
            }
        }

        [Display(Name = "Описание")]
        public string SenderNote { get; set; }

        [Display(Name = "Данни за контакт")]
        public string SenderContact { get; set; }
    }
}