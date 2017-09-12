using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Public.Portal.Models
{
    public class ComplaintModel
    {
        public ComplaintModel()
        {
            IsAnonymous = false;
            IncidentDateTime = DateTime.Now;
        }

        public DateTime SubmitDateTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime IncidentDateTime { get; set; }

        // Избор на услуга (от списъка с дефинирани услуги)
        public string ServiceName { get; set; }

        [Required]
        public int ServiceId { get; set; }

        public IdNameSelectList Services { get; set; }

        // Избор на служител(от служителите за съответната администрация);
        public string EmployeeName { get; set; }

        public string EmployeeId { get; set; }

        public CodeNameSelectList Employees { get; set; }

        // Избор на администрация (от списъка с администрации, за които има регистриран локален администратор)
        public string AdministrationName { get; set; }

        [Required]
        public int AdministrationId { get; set; }

        public IdNameSelectList Administrations { get; set; }


        [Required]
        public string SenderPidTypeCode { get; set; }

        public CodeNameSelectList PidTypes { get; set; }

        [Required]
        public string SenderIdentifier { get; set; }

        public string SenderContact { get; set; }

        public string SenderNote { get; set; }


        public bool IsAnonymous { get; set; }
    }
}