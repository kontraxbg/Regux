using Kontrax.Regux.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Kontrax.Regux.Public.Portal.Models
{
    public class ComplaintViewModel
    {
        public ComplaintViewModel()
        {
        }

        public int Id { get; set; }

        [Display(Name = "Услуга")]
        public string ServiceName { get; set; }

        [Display(Name = "Администрация")]
        public string AdministrationName { get; set; }

        [Display(Name = "Служител")]
        public string EmployeeName { get; set; }

        [Display(Name = "Дата събитие")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = false, DataFormatString = "{0:dd.MM.yyyy HH:mm г.}")]
        public DateTime IncidentDateTime { get; set; }

        [Display(Name = "Подадена жалба")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = false, DataFormatString = "{0:dd.MM.yyyy HH:mm г.}")]
        public DateTime SubmitDateTime { get; set; }

        [Display(Name = "Иден. №")]
        public string SenderIdentifier { get; set; }

        [Display(Name = "Вид")]
        public string SenderPidTypeCode { get; set; }

        [Display(Name = "Описание")]
        public string SenderNote { get; set; }

        [Display(Name = "Данни за контакт")]
        public string SenderContact { get; set; }
    }
}