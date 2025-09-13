using IFMIS.DAL;
using IFMIS.Areas.ALS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.Libraries;
using IFMIS.Services;

namespace IFMIS.Areas.ALS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IFMISTZDbContext db = new IFMISTZDbContext();
        private readonly IReportManager reportManager;
        public ReportsController()
        {

        }

        public ReportsController(
             IReportManager reportManager
            )
        {
            this.reportManager = reportManager;
        }


        public ActionResult ALS_py15agencyFeedtlRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_py15agencyFeeRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        public ActionResult ALS_InstitutionPaidFundRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_py15FinancialInstFeeRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_IndividualLoanhcmisRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        /*************Portal Report********************/

        public ActionResult ALS_PortalUserDetailRptv()
        {
            return View();
        }
        /************* End of Report Portal********************/



        public ActionResult ALS_py15agencyDeductiondtlRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }


        public ActionResult ALS_loanRepayScheduleRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_FundAllocationRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_FundBalanceRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_loanPaymentDtlRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_loanOutrightDtlRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_TraReleaseLetterRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_TraPurchaseLetterRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_loanLedgerEmployee()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_loanClearanceRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_LoanClearanceByCashRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_LoanClearByCashSummaryRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_loanClearanceListRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_LoanLedgerAgeRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        public ActionResult ALS_loanClearanceByListRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        public ActionResult ALS_StoploanDed()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_py15AgencyTransferRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_LoanDedStartRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_LoanDedStopRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_OutrightLetterRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_OutrightLetterTraRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_DetailedLoanAppRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_SummaryLoanAppRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_DetailedLoanApplicationAgingRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_DetailedLoanRecoveryRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_InstitutionFundDesbursementRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        public ActionResult ALS_ProtectionServiceChargeRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_SummaryLoanRecoveryRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_CertificateofAllocationRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_DeductionSummaryByVoteRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_InstitutionUnpaidFundRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_BatchPaymentRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        public ActionResult ALS_SalesofGovernmentassetRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_FundDisbursementStopPayRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_FundReallocationRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        public ActionResult ALS_DetailedLoanCompletedRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        public ActionResult ALS_FundAllocSummaryRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }

        public ActionResult ALS_LoanDefaultersRPTv()
        {
            var vm = new InstitutionListVm
            {
                UrlName = reportManager.GetReportUrl("ALS")
            };

            return View(vm);
        }
        
        public ActionResult LoanRepayScheduleModRPTv(int loanAppId)
        {
            var query = "loanAppId=" + loanAppId;
            var encrypted = ParameterEncryption(query);
            var UrlName = reportManager.GetReportUrl("ALS");
            var url = UrlName + "ALS_loanRepayScheduleModRPT.aspx" + encrypted;
            return Redirect(url);
        }

        //public ActionResult LoanRepayScheduleModRPTv(int loanAppId)
        //{
        //    var query = "loanAppId=" + loanAppId;
        //    var encrypted = ParameterEncryption(query);
        //    var UrlName = ReportManager.GetReportUrl(db, "ALS");
        //    var url = UrlName + "ALS_loanRepayScheduleModRPT.aspx" + encrypted;
        //    return Redirect(url);
        //}


        public ActionResult GetPy15Categories()
        {
            var pyCategories = db.ALS_PY15VoteGroups
                                 .DistinctBy(v => v.VoteGroupName)
                                 .OrderBy(v => v.VoteGroupName).ToList();
            ViewBag.report = new SelectList(pyCategories, "VoteGroupId", "VoteGroupName");
            return PartialView("_ReportPy15VoteGroup");
        }
		
		




        [HttpGet]
        public JsonResult GetPy15Agencies(string searchTerm)
        {

            List<Select2DTOString> agencies = new List<Select2DTOString>();

            if (!searchTerm.Equals("ALL", StringComparison.CurrentCultureIgnoreCase))
            {
                var agencyList = db.FinancialInstitutions
                .Where(b => b.AgencyCode.Contains(searchTerm) || b.AgencyDesc.Contains(searchTerm))
                .OrderBy(b => b.AgencyCode)
                .DistinctBy(b => b.AgencyCode)
                .Select(b => new
                {
                    b.AgencyCode,
                    b.AgencyDesc
                }).ToList();

                foreach (var agency in agencyList)
                {
                    agencies.Add(new Select2DTOString(agency.AgencyCode, (agency.AgencyCode + " - " + agency.AgencyDesc)));
                }

                return Json(new { agencies }, JsonRequestBehavior.AllowGet);
            }

            agencies.Add(new Select2DTOString("ALL", "ALL"));

            return Json(new { agencies }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPy15Votes(string searchTerm)
        {

            List<Select2DTOString> votes = new List<Select2DTOString>();

            if (!searchTerm.Equals("ALL", StringComparison.CurrentCultureIgnoreCase))
            {
                var voteList = db.PY15Votes
                .Where(b => b.CompanyCode.Contains(searchTerm) || b.CompanyDesc.Contains(searchTerm))
                .OrderBy(b => b.CompanyCode)
                .DistinctBy(b => b.CompanyCode)
                .Select(b => new
                {
                    b.CompanyCode,
                    b.CompanyDesc
                }).ToList();

                foreach (var vote in voteList)
                {
                    votes.Add(new Select2DTOString(vote.CompanyCode, (vote.CompanyCode + " - " + vote.CompanyDesc)));
                }

                return Json(new { votes }, JsonRequestBehavior.AllowGet);
            }

            votes.Add(new Select2DTOString("ALL", "ALL"));

            return Json(new { votes }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public string ParameterEncryption(string query)
        {
            query = QueryStringModule.Encrypt(query);
            return query;
        }

    }
}