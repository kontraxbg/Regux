using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontrax.Regux.Model.Audit
{
    public class AuditModel
    {
        public AuditModel()
        {
        }

        public int? ID { get; set; }
        [Display(Name = "Допълнителни данни")]
        public string Data { get; set; }

        [Display(Name = "IP Адрес")]
        public string IPAddress { get; set; }
        [Display(Name = "Сесия")]
        public string SessionID { get; set; }
        [Display(Name = "Дата и час")]
        public DateTime TimeAccessed { get; set; }
        [Display(Name = "URL")]
        public string URLAccessed { get; set; }
        [Display(Name = "Метод")]
        public string RequestMethod { get; set; }
        [Display(Name = "Потребителско име")]
        public string UserName { get; set; }
        [Display(Name = "Потребител")]
        public string UserID { get; set; }
        [Display(Name = "Контролер")]
        public string Controller { get; set; }
        [Display(Name = "Action")]
        public string Action { get; set; }
        [Display(Name = "Действие")]
        public string Activity { get; set; }
        public long DurationTicks { get; set; }
        [Display(Name = "Продължителност")]
        public TimeSpan Duration { get; set; }
    }

    public class AuditViewModel : AuditModel
    {
        public new TimeSpan Duration { get { return TimeSpan.FromTicks(DurationTicks); } }
    }

    public class AuditSearchModel
    {
        [Display(Name = "От дата")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "До дата")]
        public DateTime? ToDate { get; set; }
        [Display(Name = "Сесия")]
        public string SessionID { get; set; }
    }
}
