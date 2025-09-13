using Elmah;
using Hangfire;
//using IFMIS.Areas.ALS.Models;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using Microsoft.Ajax.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
namespace IFMIS.Libraries
{
    public class CoaService
    {
        public static ProcessResponse generateCoa(IFMISTZDbContext db, string glAccount, System.Security.Principal.IPrincipal loggedInUser, Institution loggedInInstitution)
        {
            ProcessResponse coaStatus = new ProcessResponse();
            coaStatus.OverallStatus = "Pending";
            try
            {

                if (glAccount == "")
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "GlAccount is Empty, please fill all details.";
                    return coaStatus;
                }

             
                int glAccountlength = glAccount.Length;

                if (glAccountlength != 77)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Invalid GL Account Format. The account '" + glAccount + "'. Contains " + glAccountlength.ToString() + " Length instead of 77 ";
                    return coaStatus;
                }


                string[] coaSegments = glAccount.Split('|');
                string vote = coaSegments[0];
                string subVoteCode = coaSegments[1];
                string trCode = coaSegments[2];
                string costCentreCode = coaSegments[3];
                string geographicalLocation = coaSegments[4];
                string facilityCode = coaSegments[5];
                string subBudgetClass = coaSegments[6];
                string project = coaSegments[7];
                string serviceOutPut = coaSegments[8];
                string activity = coaSegments[9];
                string fundType = coaSegments[10];
                string cofog = coaSegments[11];
                string fundingSource = coaSegments[12];
                string gfsCode = coaSegments[13];


                if (coaSegments.Count() != 14)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Invalid GL Account Format. The account '" + glAccount + "'. Contains " + coaSegments.Count().ToString() + " segments instead of 14 segments";
                    return coaStatus;
                }


                Institution institution = db.Institution
                    .Where(a => a.VoteCode == vote.Replace(" ","").Replace("  ", "").Replace("   ", "")
                    && a.Level2Code == trCode.Replace(" ", "").Replace("  ", "").Replace("   ", "")
                    && a.Level3Code == geographicalLocation 
                    && a.OverallStatus == "Active").FirstOrDefault();



                if (facilityCode == "00000000") // No facility
                {
                    institution = db.Institution.Where(a => a.VoteCode == vote
                    && a.Level2Code == trCode
                    //  && a.Level3Code == geographicalLocation
                    ).FirstOrDefault();
                    if (institution == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "Institution Setup does not match COA segment setup for vote '"
                            + vote + "', TR '" + trCode;//+ "' and geographical location '"
                                                        //  + geographicalLocation + "'.";
                        return coaStatus;
                    }
                }
                else
                {
                    institution = db.Institution.Where(a => a.VoteCode == vote
                                       && a.Level2Code == trCode
                                       // && a.Level3Code == geographicalLocation
                                       // && a.Level4Code == facilityCode
                                       ).FirstOrDefault();
                    if (institution == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "Institution Setup does not match COA segment setup for vote '"
                            + vote + "', TR '" + trCode// + "' and geographical location '"
                                                       // + geographicalLocation
                                                       // + " and facility code " + facilityCode
                            + "'.";
                        return coaStatus;
                    }
                }
                //  InstitutionSubLevel user = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());

                institution = loggedInInstitution;
                int InstitutionId = institution.InstitutionId;

                if (db.COAs.Where(a => a.GlAccount.Replace(" ","").Replace("  ","") == glAccount.Replace(" ", "").Replace("  ", "") && a.InstitutionId == InstitutionId && a.Status.ToUpper() != "CANCELLED").Count() > 0)
                {
                    coaStatus.OverallStatus = "Exists";
                    coaStatus.OverallStatusDescription = "This GL Account Exists: " + glAccount;
                    return coaStatus;
                }

                if (CoaCheck(vote, 1, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Vote Does not Exist: " + vote;
                    return coaStatus;
                }

                //if (CoaCheck(subVoteCode, 2, db) == 0)
                //{
                //    coaStatus.OverallStatus = "Error";
                //    coaStatus.OverallStatusDescription = "SubVote Does not Exist: " + subVoteCode;
                //    return coaStatus;
                //}

                if (CoaCheck(trCode, 3, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "TR Does not Exist: " + trCode;
                    return coaStatus;
                }

                //if (CoaCheck(costCentreCode, 4, db) == 0)
                //{
                //    coaStatus.OverallStatus = "Error";
                //    coaStatus.OverallStatusDescription = "Cost Centre Does not Exist: " + costCentreCode;
                //    return coaStatus;
                //}

                if (CoaCheck(geographicalLocation, 5, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Geographical Location Centre Does not Exist: " + geographicalLocation;
                    return coaStatus;
                }

                if (CoaCheck(facilityCode, 6, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Facility Does not Exist: " + facilityCode;
                    return coaStatus;
                }

                if (CoaCheck(subBudgetClass, 7, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "SubBudget Class Does not Exist: " + subBudgetClass;
                    return coaStatus;
                }

                if (CoaCheck(project, 8, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Project Does not Exist: " + project;
                    return coaStatus;
                }
                var activeCoaVersion = db.CoaVersions
                    .Where(a => a.OverallStatus.ToUpper() == "ACTIVE")
                    .FirstOrDefault();

                if (CoaCheck(serviceOutPut, 9, db) == 0)
                {
                    if (serviceOutPut.Length != 3)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "The service output code '" + serviceOutPut
                            + "' has the wrong length of " + serviceOutPut.Length.ToString() + ". The proper length is 3 characters.";
                        return coaStatus;
                    }
                    if (activeCoaVersion == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "There is no active COA version. Please consult system Administrator!";
                        return coaStatus;
                    }

                    var serviceOutputCoaSegment = new CoaSegment
                    {
                        CoaVersionId = activeCoaVersion.CoaVersionId,
                        SegmentCode = serviceOutPut,
                        SegmentName = "SERVICE OUTPUT",
                        SegmentDesc = "",
                        SegmentNo = 9,
                        SegmentLength = 3,
                        CreatedBy = loggedInUser.Identity.Name,
                        CreatedAt = DateTime.Now,
                    };

                    db.CoaSegments.Add(serviceOutputCoaSegment);
                }
                if (CoaCheck(activity, 10, db) == 0)
                {
                    if (activity.Length != 6)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "The code code '" + activity
                            + "' has the wrong length of " + activity.Length.ToString() + ". The proper length is 6 characters.";
                        return coaStatus;
                    }
                    if (activeCoaVersion == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "There is no active COA version. Please consult system Administrator!";
                        return coaStatus;
                    }

                    var activityCoaSegment = new CoaSegment
                    {
                        CoaVersionId = activeCoaVersion.CoaVersionId,
                        SegmentCode = activity,
                        SegmentName = "ACTIVITY",
                        SegmentDesc = "",
                        SegmentNo = 10,
                        SegmentLength = 6,
                        CreatedBy = loggedInUser.Identity.Name,
                        CreatedAt = DateTime.Now,
                    };

                    db.CoaSegments.Add(activityCoaSegment);
                }
                if (CoaCheck(fundType, 11, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Error adding gl account '" + glAccount + "'. fund type '" + fundType + "' does not Exist.";
                    return coaStatus;
                }
                if (CoaCheck(cofog, 12, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Error adding gl account '" + glAccount + "'. COFOG '" + cofog + "' does not Exist.";
                    return coaStatus;
                }
                if (CoaCheck(fundingSource, 13, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Error adding gl account '" + glAccount + "'. Funding Source '" + fundingSource + "' does not Exist.";
                    return coaStatus;
                }
                if (CoaCheck(gfsCode, 14, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "Error adding gl account '" + glAccount + "'. GfsCode '" + gfsCode + "' does not Exist.";
                    return coaStatus;
                }


                //string subVoteDesc = db.CoaSegments.
                //    Where(a => a.SegmentCode == subVoteCode
                //    && a.SegmentName == "SUB VOTE").FirstOrDefault().SegmentDesc;
                ////TODO: Review sub level description for Ministries and other Main Votes
       

                string costCentreDesc = "N/A";
                string subVoteDesc = "N/A";


                if (subVoteCode != "0000" && institution.InstitutionLevel == 1) //MDA
                {
                    InstitutionSubLevel subvote = db.InstitutionSubLevels
                                                .Where(a => a.InstitutionCode == institution.InstitutionCode
                                                && a.SubLevelCategory == "SUB VOTE"
                                                && a.SubLevelCode == subVoteCode).FirstOrDefault();
                    if (subvote == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "Error adding gl account '" + glAccount + "'. Paystation for institutionCode '" + institution.InstitutionCode + "' and Sub Vote '" + subVoteCode + "' does not exist.";
                        return coaStatus;
                    }
                    subVoteDesc = subvote.SubLevelDesc;
                }
                if (costCentreCode != "0000")
                {
                    InstitutionSubLevel costCentre = db.InstitutionSubLevels
                                         .Where(a => a.InstitutionCode == institution.InstitutionCode
                                         && a.SubLevelCategory == "COST CENTRE"
                                         && a.SubLevelCode == costCentreCode).FirstOrDefault();
                    if (costCentre == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "Error adding gl account '" + glAccount + "'. Paystation for institutionCode '" + institution.InstitutionCode + "' and cost Centre '" + costCentreCode + "' does not exist.";
                        return coaStatus;
                    }
                    costCentreDesc = costCentre.SubLevelDesc;
                }



                string facilityDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == facilityCode
                    && a.SegmentName == "FACILITY").FirstOrDefault().SegmentDesc;

                string gfsDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == gfsCode
                    && a.SegmentName == "GFS CODE").FirstOrDefault().SegmentDesc;

                string fundTypeDesc = db.CoaSegments.
                   Where(a => a.SegmentCode == fundType
                   && a.SegmentName == "FUND TYPE").FirstOrDefault().SegmentDesc;

                string fundingSourceDesc = db.CoaSegments.
                   Where(a => a.SegmentCode == fundingSource
                   && a.SegmentName == "FUNDING SOURCE").FirstOrDefault().SegmentDesc;

                string geographicalLocationDesc = db.CoaSegments.
                   Where(a => a.SegmentCode == geographicalLocation
                   && a.SegmentName == "GEOGRAPHICAL LOCATION").FirstOrDefault().SegmentDesc;

                string trDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == trCode
                    && a.SegmentName == "TR/COUNCIL").FirstOrDefault().SegmentDesc;

                string projectDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == project
                    && a.SegmentName == "PROJECT").FirstOrDefault().SegmentDesc;

                string voteDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == vote
                    && a.SegmentName == "VOTE").Select(a => a.SegmentDesc).FirstOrDefault();


                string cofogDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == cofog
                    && a.SegmentName == "COFOG").Select(a => a.SegmentDesc).FirstOrDefault();


                string subBudgetClassDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == subBudgetClass
                    && a.SegmentName == "SUB BUDGET CLASS").Select(a=>a.SegmentDesc).FirstOrDefault();


                string serviceoutputDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == serviceOutPut
                    && a.SegmentName == "SERVICE OUTPUT").Select(a => a.SegmentDesc).FirstOrDefault();



                COA newCoa = new COA
                {
                    CoaVersionId = activeCoaVersion.CoaVersionId,
                    Vote = vote,
                    SubVote = subVoteCode,
                    SubVoteDesc = subVoteDesc,
                    TR = trCode,
                    CostCentre = costCentreCode,
                    CostCentreDesc = costCentreDesc,
                    GeographicalLocation = geographicalLocation,
                    Facility = facilityCode,
                    FacilityDesc = facilityDesc,
                    SubBudgetClass = subBudgetClass,
                    Project = project,
                    ServiceOutput = serviceOutPut,
                    Activity = activity,
                    FundType = fundType,
                    COFOG = cofog,
                    FundingSource = fundingSource,
                    GfsCode = gfsCode,
                    GFSDesc = gfsDesc,
                    GlAccount = glAccount.Replace(" ",""),
                    GlAccountDesc = gfsDesc,
                    CreatedBy = loggedInUser.Identity.Name,
                    CreatedAt = System.DateTime.Now,
                    InstitutionId = institution.InstitutionId,
                    InstitutionCode=institution.InstitutionCode,
                    InstitutionName=institution.InstitutionName,
                    InstitutionLevel=institution.InstitutionLevel,
                    InstitutionLogo=institution.InstitutionLogo,
                    Level1Code=institution.Level1Code,
                    Level1Desc=institution.Level1Desc,
                    subBudgetClassDesc=subBudgetClassDesc,
                    ServiceOutputDesc= serviceoutputDesc,
                    ActivityDesc= "NA",
                    GfsCodeCategory = "Expenses",
                    Status = "ACTIVE",
                    FundTypeDesc = fundTypeDesc,
                    FundingSourceDesc = fundingSourceDesc,
                    GeographicalLocationDesc = geographicalLocationDesc,
                    TrDesc = trDesc,
                    ProjectDesc = projectDesc,
                    VoteDesc = voteDesc,
                    CofogDesc = cofogDesc,
                };

                db.COAs.Add(newCoa);
                db.SaveChanges();

                var parameters = new SqlParameter[] { new SqlParameter("@InstitutionCode", institution.InstitutionCode) };
                db.Database.ExecuteSqlCommand("PostMissingJournalTypeCoa_p @InstitutionCode", parameters);


            }
            catch (Exception ex)
            {
                coaStatus.OverallStatus = "Error";
                coaStatus.OverallStatusDescription = ex.Message.ToString();
                return coaStatus;
            }
            return coaStatus;
        }
        public static int CoaCheck(string segmentCode, int segementno, IFMISTZDbContext db)
        {
            int count = 0;
            count = db.CoaSegments.Where(ab => ab.SegmentCode == segmentCode && ab.SegmentNo == segementno).Count();
            return count;
        }
    }
}