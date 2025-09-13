namespace IFMIS.Areas.IFMISTZ.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Web;
    using System.Web.Mvc;

    public partial class UploadBankStatementVM
    {
        public int BankStatementSummaryId { get; set; }
        [Display(Name = "Account Number")]
        public string BankAccountNumber { get; set; }
        public IEnumerable<SelectListItem> AccountNumberNameList { get; set; }
        [Display(Name = "Bank Name")]
        public string bankName { get; set; }
        public IEnumerable<SelectListItem> bankNameList { get; set; }
        public DateTime? StatementDate { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }

        [StringLength(250)]
        public string TransactionRef { get; set; }

        [StringLength(250)]
        public string RelatedRef { get; set; }
        [StringLength(250)]
        public string Description { get; set; }
        [Display(Name = "File Name")]
        public HttpPostedFileBase FileName { get; set; }
        //public string ValueDate { get; set; }
        public DateTime ValueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal CommulativeBalance { get; set; }
   


    }
}
