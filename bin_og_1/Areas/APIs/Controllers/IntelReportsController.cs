using IFMIS.DAL;
using IFMIS.Libraries;
using IFMIS.Models;
using IFMIS.Services;
using Microsoft.AspNet.Identity;
using System.Linq;
using System.Web.Mvc;

namespace IFMIS.Controllers
{
    [Authorize]
    public class IntelReportsController : Controller
    {
        private readonly IFMISTZDbContext db = new IFMISTZDbContext();
        private readonly GACSDbContext _db = new GACSDbContext();
        private readonly IPayStation payStationServices;
        private readonly IServiceManager serviceManager;

        public IntelReportsController()
        {

        }

        public IntelReportsController(
            IPayStation payStationServices,
            IServiceManager serviceManager
            )
        {
            this.payStationServices = payStationServices;
            this.serviceManager = serviceManager;
        }

        // GET: IFMISTZ/Reports
        [HttpGet]
        public ActionResult ReportsManager()
        {
            var reportManager = new ReportManagerVM
            {
                ReportCategoryIds = new SelectList(db.ReportCategories, "ReportCategoryId", "CategoryName"),
                ReportSubCategoryIds = new SelectList(""),
                ReportIds = new SelectList("")
            };

            return View(reportManager);
        }

        [HttpGet]
        public ActionResult QuickReport()
        {
            IQueryable<Report> reports;

            reports = db.Reports;
            if (!User.IsInRole("Consolidator"))
            {
                reports = reports.Where(a => a.RoleName == "All");
            }

            var quickReport = new QuickReportVM
            {
                ReportIds = new SelectList(reports.OrderBy(a => a.ReportName), "ReportId", "ReportName")
            };

            return View(quickReport);
        }

        [HttpGet]
        public ActionResult GetReportPartialView(int id)
        {
            var report = db.Reports.Find(id);
            var voteCriteria = new string[] { "GOT", "Entity Sub Sector", "Specific Entity List" };
            var reportTypes = new string[] { "All", "Position", "Cash Flow", "Performance" };
            var itemTypes = new string[] {"Item", "Entity" };
            var ageAnalyses = new string[] { "Payable", "Prepayment", "Receivable", "Credit Risk", "Liquidity Risk" };
            var notes = _db.GacsNotes.OrderBy(a => a.NoteNo);
            var assetMovements = new string[] { "PPE", "Intangible", "Investment Property", "WIP" };
            var consolidationStatue = new string[] { "All", "Posted", "Unposted" };
            var currencyAnalyses = new string[] { "All" };//Multi dimension
            var elimiationEntries = new string[] { "All", "Performance", "Position", "Cash Flow" };//Multi dimension
            var financialYears = db.FinancialYears
                .Where(a => a.OverallStatus=="Active")
                .OrderByDescending(a => a.FinancialYearCode)
                .ToList();

            if (report.ReportView == "_EntityListReportParameters")
            {
                var reportParams = new EntityListReportParametersVM
                {
                    SubSectors = new SelectList(""),
                    ReportPath = report.ReportPath
                };

                return PartialView(report.ReportView, reportParams);
            }

            if (report.ReportView == "_ClassificationSubChapterReportParameters")
            {
                var reportParams = new ClassificationSubChapterReportParametersVM
                {
                    Classifications = new SelectList(""),
                    SubChapters = new SelectList(""),
                    ReportPath = report.ReportPath
                };

                return PartialView(report.ReportView, reportParams);
            }

            var reportParam = new FyReportParametersVM
            {
                FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                VoteCode = "0",
                ReportPath = report.ReportPath
            };

            var userPayStation = payStationServices.GetDefaultUserPayStation(User.Identity.GetUserId());

            var institutions = (from u in db.Institution
                                where u.OverallStatus == "Active"
                                where u.InstitutionCode == userPayStation.InstitutionCode
                                select new
                                {
                                    u.InstitutionId,
                                    u.InstitutionCode,
                                    u.InstitutionName,
                                    u.GacsVoteCode
                                }).OrderByDescending( a=> a.InstitutionName).ToList();

            if (report.ReportView == "_GeneralReportParameters")
            {
                reportParam = new GeneralReportParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCodes = new SelectList(serviceManager.GetSelectListItems(voteCriteria), "value", "text"),
                    EntitySectors = new SelectList(""),
                    EntitySubSectors = new SelectList(""),
                    Entities = User.IsInRole("Consolidator") ? new SelectList("") : new SelectList(institutions, "GacsVoteCode", "InstitutionName", userPayStation.GacsVoteCode),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_VoteReportTypeParameters")
            {
                reportParam = new VoteReportTypeParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCodes = new SelectList(serviceManager.GetSelectListItems(voteCriteria), "value", "text"),
                    EntitySectors = new SelectList(""),
                    EntitySubSectors = new SelectList(""),
                    Entities = User.IsInRole("Consolidator") ? new SelectList("") : new SelectList(institutions, "GacsVoteCode", "InstitutionName", userPayStation.GacsVoteCode),
                    ReportTypes = new SelectList(serviceManager.GetSelectListItems(reportTypes), "value", "text"),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_VoteItemReportTypeParameters")
            {
                reportParam = new VoteItemReportTypeParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCodes = new SelectList(serviceManager.GetSelectListItems(voteCriteria), "value", "text"),
                    EntitySectors = new SelectList(""),
                    EntitySubSectors = new SelectList(""),
                    Entities = User.IsInRole("Consolidator") ? new SelectList("") : new SelectList(institutions, "GacsVoteCode", "InstitutionName", userPayStation.GacsVoteCode),
                    ItemTypes = new SelectList(serviceManager.GetSelectListItems(itemTypes), "value", "text"),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_VoteNoteReportParameters")
            {
                reportParam = new VoteNoteReportParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCodes = new SelectList(serviceManager.GetSelectListItems(voteCriteria), "value", "text"),
                    EntitySectors = new SelectList(""),
                    EntitySubSectors = new SelectList(""),
                    Entities = User.IsInRole("Consolidator") ? new SelectList("") : new SelectList(institutions, "GacsVoteCode", "InstitutionName", userPayStation.GacsVoteCode),
                    NoteNos = new SelectList(notes, "NoteNo", "NoteNoNoteDesc"),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_VoteAgeAnalysisReportParameters")
            {
                reportParam = new VoteAgeAnalysisReportParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCodes = new SelectList(serviceManager.GetSelectListItems(voteCriteria), "value", "text"),
                    EntitySectors = new SelectList(""),
                    EntitySubSectors = new SelectList(""),
                    Entities = User.IsInRole("Consolidator") ? new SelectList("") : new SelectList(institutions, "GacsVoteCode", "InstitutionName", userPayStation.GacsVoteCode),
                    AgeAnalyses = new SelectList(serviceManager.GetSelectListItems(ageAnalyses), "value", "text"),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_VoteAssetMovementReportParameters")
            {
                reportParam = new VoteAssetMovementReportParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCodes = new SelectList(serviceManager.GetSelectListItems(voteCriteria), "value", "text"),
                    EntitySectors = new SelectList(""),
                    EntitySubSectors = new SelectList(""),
                    Entities = User.IsInRole("Consolidator") ? new SelectList("") : new SelectList(institutions, "GacsVoteCode", "InstitutionName", userPayStation.GacsVoteCode),
                    AssetMovements = new SelectList(serviceManager.GetSelectListItems(assetMovements), "value", "text"),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_VoteCurrencyAnalysisReportParameters")
            {
                reportParam = new VoteCurrencyAnalysisReportParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCodes = new SelectList(serviceManager.GetSelectListItems(voteCriteria), "value", "text"),
                    EntitySectors = new SelectList(""),
                    EntitySubSectors = new SelectList(""),
                    Entities = User.IsInRole("Consolidator") ? new SelectList("") : new SelectList(institutions, "GacsVoteCode", "InstitutionName", userPayStation.GacsVoteCode),
                    CurrencyAnalyses = new SelectList(serviceManager.GetSelectListItems(currencyAnalyses), "value", "text"),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_ConsolidationStatusReportParameters")
            {
                reportParam = new ConsolidationStatusReportParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    VoteCode = "0",
                    ConsolidationStatue = new SelectList(serviceManager.GetSelectListItems(consolidationStatue), "value", "text"),
                    ReportPath = report.ReportPath
                };
            }
            else if (report.ReportView == "_ReportTypeReportParameters")
            {
                reportParam = new ReportTypeReportParametersVM
                {
                    FinancialYears = new SelectList(financialYears, "FinancialYearCode", "FinancialYearCode"),
                    ReportTypes = new SelectList(serviceManager.GetSelectListItems(elimiationEntries), "value", "text"),
                    ReportPath = report.ReportPath
                };
            }

            return PartialView(report.ReportView, reportParam);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}