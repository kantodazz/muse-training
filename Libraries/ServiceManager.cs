using Elmah;
using Hangfire;
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
    public static class ServiceManager
    {
        public static void LogAuditTrail(DbContext db, string username, string actionType, string actionDesc, DateTime actionAt)
        {
            //AuditTrail auditTrail = new AuditTrail();
            //auditTrail.Username = username;
            //auditTrail.ActionType = actionType;
            //auditTrail.ActionDesc = actionDesc;
            //auditTrail.ActionAt = actionAt;
            //db.Entry(auditTrail).State = EntityState.Added;
            //db.SaveChanges();
        }

        internal static object GetInstitutionList(IFMISTZDbContext db, IFMISTZDbContext iFMISTZDbContext, object p)
        {
            throw new NotImplementedException();
        }

        internal static object GetInstitutionList(IFMISTZDbContext db, IFMISTZDbContext iFMISTZDbContext, string v)
        {
            throw new NotImplementedException();
        }

        internal static object GetInstitutionCurrencyList(IFMISTZDbContext db, string v)
        {
            throw new NotImplementedException();
        }


        internal static List<InstitutionAccount> GetAccountListrec(IFMISTZDbContext db, string institutionCode)
        {
            List<InstitutionAccount> Accounts = db.InstitutionAccounts.Where(a => a.InstitutionCode == institutionCode
            && a.OverallStatus != "Cancelled").ToList();
            return Accounts;
        }

        internal static List<Bank> GetBankList(IFMISTZDbContext db)
        {
            List<Bank> bank = db.Banks.ToList();

            return bank;
        }

        internal static object GetLowerEndOfFY(object gsrpdb)
        {
            throw new NotImplementedException();
        }

        public static string GetFinancialYearString(IFMISTZDbContext db, DateTime currentMonth)
        {
            ///DateTime currentMonth = DateTime.Now;
            string financialYearString;
            var selectedDate = db.FinancialYears.Where(a => currentMonth >= a.FinancialYearStartDate && currentMonth <= a.FinancialYearEndDate && a.OverallStatus == "ACTIVE").FirstOrDefault();
            int month = currentMonth.Month;

            if (selectedDate == null)
            {
                financialYearString = "NoValidFinancialYear";
                return financialYearString;
            }

            financialYearString = selectedDate.FinancialYearDesc;

            return financialYearString;
        }

        public static int GetFinancialPeriod(DateTime dt)
        {
            int period = 0;

            if (dt.Month > 6)
            {
                period = dt.Month - 6;
            }
            else
            {
                period = dt.Month + 6;
            }
            return period;
        }

        internal static bool IsUserHasPayStation(IFMISTZDbContext db, string userName)
        {
            var userId = db.Users.Where(a => a.UserName == userName).Select(a => a.Id).FirstOrDefault();
            var count = db.UserPayStations.Where(a => a.UserId == userId && a.IsDefault == true).Count();
            if (userId != null && count == 0)
            {
                return false;
            }

            return true;
        }

        //public static int GetFinancialYear()
        //{
        //    DateTime currentMonth = DateTime.Now;

        //    int month = currentMonth.Month;

        //    if (month >= 7)
        //    {
        //        return currentMonth.Year + 1;
        //    }

        //    return (currentMonth.Year);
        //}
        //public static int GetFinancialYear(DateTime currentMonth)
        //{

        //    int month = currentMonth.Month;

        //    if (month >= 7)
        //    {
        //        return currentMonth.Year + 1;
        //    }

        //    return (currentMonth.Year);
        //}

        public static int GetFinancialYear(IFMISTZDbContext db, DateTime currentMonth)
        {
            int financialYear = 0;
            var selectedDate = db.FinancialYears
                .Where(a => DbFunctions.TruncateTime(currentMonth) >= DbFunctions.TruncateTime(a.FinancialYearStartDate)
                  && DbFunctions.TruncateTime(currentMonth) <= DbFunctions.TruncateTime(a.FinancialYearEndDate)
                  && a.OverallStatus == "ACTIVE")
                 .FirstOrDefault();
            int month = currentMonth.Month;

            if (selectedDate == null)
            {
                return -1;
            }

            financialYear = selectedDate.FinancialYearCode;


            return (financialYear);
        }

        public static string GeneratePassword()
        {
            return "@User123";
        }

        public static void SendEmail(string from, string to, string subject, string msgBody)
        {
            var body = "<p>Email From: {0} ({1})</p><p>Message:</p><p>{2}</p>";
            var message = new MailMessage();
            message.From = new MailAddress(from);  // replace with valid value
            message.To.Add(new MailAddress(to));  // replace with valid value 
            message.Subject = subject;
            message.Body = string.Format(body, from, to, msgBody);
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Send(message);
            }
        }


        internal static List<FundSourceTypeVM> GetFundSourceType(IFMISTZDbContext db, int fundsourceId)
        {
            var fundSourceType = (from u in db.FundSourceTypes
                                  where u.FundSourceId == fundsourceId
                                  select new FundSourceTypeVM
                                  {
                                      FundTypeName = u.FundTypeName,
                                  }).ToList();

            return fundSourceType;
        }


        // This is one of the most important parts in the whole example.
        // This function takes a list of strings and returns a list of SelectListItem objects.
        // These objects are going to be used later in the SignUp.html template to render the
        // DropDownList.
        public static IEnumerable<SelectListItem> GetSelectListItems(IEnumerable<string> elements)
        {
            // Create an empty list to hold result of the operation
            var selectList = new List<SelectListItem>();

            // For each string in the 'elements' variable, create a new SelectListItem object
            // that has both its Value and Text properties set to a particular value.
            // This will result in MVC rendering each item as:
            //     <option value="State Name">State Name</option>
            foreach (var element in elements)
            {
                selectList.Add(new SelectListItem
                {
                    Value = element,
                    Text = element
                });
            }

            return selectList;
        }

        internal static List<FundReceivingVM> GetFundBudgetClass(IFMISTZDbContext db, string fundtype)
        {
            var budgetClass = (from u in db.FundReceivings
                               where u.FundType == fundtype
                               select new FundReceivingVM
                               {
                                   BudgetClass = u.BudgetClass,
                               }).AsEnumerable().DistinctBy(x => x.BudgetClass).ToList();

            return budgetClass;
        }


        internal static List<InstitutionAccountVM> GetBankAccount(IFMISTZDbContext db, string subBudgetClass, string institutionCode)
        {
            var budgetClass = (from u in db.InstitutionAccounts
                               where u.InstitutionCode == institutionCode
                               where u.SubBudgetClass == subBudgetClass
                               //where u.AccountNumber == accountNumber
                               where u.OverallStatus != "Cancelled"
                               select new InstitutionAccountVM
                               {
                                   AccountNumber = u.AccountNumber,
                                   AccountName = u.AccountNumber + " - " + u.AccountName
                               }).AsEnumerable().DistinctBy(x => x.AccountNumber).ToList();

            return budgetClass;
        }


        internal static List<ConsolidationGroupView> GetWithinInstitutions(IFMISTZDbContext db, string institutionCode)
        {
            List<ConsolidationGroupView> institutions = null;
            if (institutionCode != "All")
            {
                institutions = (from u in db.ConsolidationGroupViews
                                where u.ConsolidationHeaderCode == institutionCode
                                select u).ToList();
            }
            else
            {
                institutions = (from u in db.ConsolidationGroupViews
                                select u).ToList();
            }
            return institutions;
        }

        internal static List<InstitutionAccountVM> GetBankAccountByInstitution(IFMISTZDbContext db, string institutionCode)
        {
            var account = (from u in db.InstitutionAccounts
                               //where u.InstitutionCode == institutionCode
                           where u.InstitutionCode.Contains(institutionCode)
                           select new InstitutionAccountVM
                           {
                               AccountNumber = u.AccountNumber,
                               AccountName = u.AccountNumber + " - " + u.AccountName
                           }).AsEnumerable().DistinctBy(a => a.AccountNumber).ToList();

            return account;
        }


        //internal static List<InstitutionVM> GetBudgetClass(IFMISTZDbContext db,int institutionId)
        //{
        //    var budgetClass = (from u in db.Institution
        //                       where u.InstitutionId == institutionId
        //                       //where u.SubBudgetClass == subBudgetClass
        //                       select new InstitutionVM
        //                       {
        //                           AccountNumber = u.AccountNumber,
        //                           AccountName = u.AccountNumber + " - " + u.AccountName
        //                       }).AsEnumerable().DistinctBy(x => x.InstitutionAccountId).ToList();

        //    return budgetClass;
        //}


        internal static List<FundReceivingVM> GetFundComponent(IFMISTZDbContext db, string fundReferenceNo)
        {
            var fundComp = (from u in db.FundReceivings
                            where u.FundingRefNo == fundReferenceNo
                            select new FundReceivingVM
                            {
                                ComponentName = u.ComponentName,
                            }).AsEnumerable().DistinctBy(x => x.ComponentName).ToList();

            return fundComp;
        }

        public static bool IsPasswdMustChange(IFMISTZDbContext db, string userName)
        {
            var isPasswdMustChange = db.Users.Where(a => a.UserName == userName).Select(b => b.IsPasswdMustChange).FirstOrDefault();
            if (isPasswdMustChange == true)
            {
                return true;
            }
            return false;
        }

        public static bool IsUserBlocked(IFMISTZDbContext db, string userName)
        {
            var isUserBlocked = db.Users.Where(a => a.UserName == userName).Select(b => b.IsBlocked).FirstOrDefault();
            if (isUserBlocked == true)
            {
                return true;
            }
            return false;
        }

        public static string GetEndDateByMonthId(string monthId, int? yearId)
        {
            string endDate = "";
            switch (monthId)
            {
                case "01":
                    endDate = "31";
                    break;
                case "02":
                    endDate = ((yearId % 4 == 0 && yearId % 100 != 0) || (yearId % 400 == 0)) ? "29" : "28";
                    break;
                case "03":
                    endDate = "31";
                    break;
                case "04":
                    endDate = "30";
                    break;
                case "05":
                    endDate = "31";
                    break;
                case "06":
                    endDate = "30";
                    break;
                case "07":
                    endDate = "31";
                    break;
                case "08":
                    endDate = "31";
                    break;
                case "09":
                    endDate = "30";
                    break;
                case "10":
                    endDate = "31";
                    break;
                case "11":
                    endDate = "30";
                    break;
                default:
                    endDate = "31";
                    break;
            }
            return endDate;
        }

        public static string SendFile(string fileName, string Ip, string url)
        {
            string response = "";

            try
            {


                //Creating Request of the destination URL
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);

                httpRequest.Method = "POST";

                //Defining the type of the posted data as XML
                httpRequest.ContentType = "application/xml";

                string data = File.ReadAllText(fileName);//"Hello World!"; //
                                                         /**  LIMITS OF THE SIZE OF STRING VARIABLE (file size)
                                                          * The theoretical limit may be 2,147,483,647, but the practical limit is nowhere near that. Since no single object in a .Net program may be over 2GB 
                                                          * and the string type uses unicode (2 bytes for each character), the best you could do is 1,073,741,823, 
                                                          * but you're not likely to ever be able to allocate that on a 32-bit machine.
                                                          * 

                                                         **/
                byte[] bytedata = Encoding.UTF8.GetBytes(data);
                // Get the request stream.
                Stream requestStream = httpRequest.GetRequestStream();
                // Write the data to the request stream.
                requestStream.Write(bytedata, 0, bytedata.Length);
                requestStream.Close();
                //Get Response
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                if (httpResponse.StatusCode.ToString() == "OK")
                {
                    StreamReader sr = new StreamReader(httpResponse.GetResponseStream());

                    File.WriteAllText(fileName + ".response", sr.ReadToEnd().Trim());//Log the http response from GePG.                        
                }
                else
                {
                    string apiResponse = "Status Code: " + httpResponse.StatusCode.ToString() + Environment.NewLine
                    + "Status Description: " + httpResponse.StatusDescription.ToString() + Environment.NewLine
                    + "ResponseUri:" + httpResponse.ResponseUri.ToString() + Environment.NewLine
                    + "Server:" + httpResponse.Server.ToString() + Environment.NewLine + Environment.NewLine
                    + "Headers:" + httpResponse.Headers.ToString()
                    + "Response Stream:" + httpResponse.GetResponseStream();

                    File.WriteAllText(fileName + ".response", apiResponse);//Log the http response from GePG.  
                }

                response = httpResponse.StatusCode.ToString();

            }
            catch (ProtocolViolationException ex)
            {
                response = ex.Message;
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (WebException ex)
            {
                response = ex.Message;
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (InvalidOperationException ex)
            {
                response = ex.Message;
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (NotSupportedException ex)
            {
                response = ex.Message;
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (Exception ex)
            {
                response = "NotOk";
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return response;
        }

        internal static BudgetClassFundTypeVM GetSubBudgetClassDescBySubBudgetClassCode(string subBudgetClass)
        {
            BudgetClassFundTypeVM response;

            switch (subBudgetClass)
            {
                case "101":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "PE", FundType = "Recurrent Expenditure" };
                    break;
                case "102":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "Other Charges", FundType = "Recurrent Expenditure" };
                    break;
                case "103":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "OwnSource", FundType = "OwnSource Recurrent" };
                    break;
                case "201":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "Local", FundType = "Development Expenditure" };
                    break;
                case "202":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "Foreign", FundType = "Development Expenditure" };
                    break;
                case "203":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "OwnSource", FundType = "OwnSource Development" };
                    break;
                case "301":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "Deposit", FundType = "Deposit Expenditure" };
                    break;
                case "501":
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "Revenue", FundType = "Revenue Collection" };
                    break;
                default:
                    response = new BudgetClassFundTypeVM { SubBudgetClassDesc = "CFS Others", FundType = "Recurrent Expenditure" };
                    break;
            }

            return response;
        }

        internal static HttpWebResponse SendToGepg(MemoryStream data, string gepgUrl, string gepgIp)
        {
            HttpWebResponse httpResponse = null;

            try
            {
                //var ping = new System.Net.NetworkInformation.Ping();

                //var result = ping.Send(gepgIp);

                //if (result.Status != System.Net.NetworkInformation.IPStatus.Success)
                //{
                //    response = "NotFound";
                //    return response;
                //}

                //Creating Request of the destination URL
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(gepgUrl);

                httpRequest.Method = "POST";

                //Defining the type of the posted data as XML
                httpRequest.ContentType = "application/xml";
                httpRequest.Headers["Gepg-Com"] = "default.sp.in";
                httpRequest.Headers["Gepg-Code"] = Properties.Settings.Default.SpCode;

                byte[] bytedata = data.ToArray();
                // Get the request stream.
                Stream requestStream = httpRequest.GetRequestStream();
                // Write the data to the request stream.
                requestStream.Write(bytedata, 0, bytedata.Length);
                requestStream.Close();
                //Get Response
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            }
            catch (ProtocolViolationException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (WebException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (InvalidOperationException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (NotSupportedException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return httpResponse;
        }

        internal static HttpWebResponse SendToGepg(string data, string gepgUrl, string gepgIp, string spCode)
        {
            HttpWebResponse httpResponse = null;

            try
            {
                //var ping = new System.Net.NetworkInformation.Ping();

                //var result = ping.Send(gepgIp);

                //if (result.Status != System.Net.NetworkInformation.IPStatus.Success)
                //{
                //    response = "NotFound";
                //    return response;
                //}

                //Creating Request of the destination URL
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(gepgUrl);

                httpRequest.Method = "POST";

                //Defining the type of the posted data as XML
                httpRequest.ContentType = "application/xml";
                httpRequest.Headers["Gepg-Com"] = "default.sp.in";
                httpRequest.Headers["Gepg-Code"] = spCode;

                byte[] bytedata = Encoding.UTF8.GetBytes(data);
                // Get the request stream.
                Stream requestStream = httpRequest.GetRequestStream();
                // Write the data to the request stream.
                requestStream.Write(bytedata, 0, bytedata.Length);
                requestStream.Close();
                //Get Response
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            }
            catch (ProtocolViolationException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (WebException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (InvalidOperationException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (NotSupportedException ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return httpResponse;
        }

        internal static object GetRecPostedCheckDate(IFMISTZDbContext db, IEnumerable<SelectListItem> institutionAccountList)
        {
            throw new NotImplementedException();
        }

        public static string NumberToText(int number, bool isUK)
        {
            if (number == 0) return "Zero";
            string and = isUK ? "and " : ""; // deals with UK or US numbering
            if (number == -2147483648) return "Minus Two Billion One Hundred " + and +
            "Forty Seven Million Four Hundred " + and + "Eighty Three Thousand " +
            "Six Hundred " + and + "Forty Eight";
            int[] num = new int[4];
            int first = 0;
            int u, h, t;
            StringBuilder sb = new StringBuilder();
            if (number < 0)
            {
                sb.Append("Minus ");
                number = -number;
            }
            string[] words0 = { "", "One ", "Two ", "Three ", "Four ", "Five ", "Six ", "Seven ", "Eight ", "Nine " };
            string[] words1 = { "Ten ", "Eleven ", "Twelve ", "Thirteen ", "Fourteen ", "Fifteen ", "Sixteen ", "Seventeen ", "Eighteen ", "Nineteen " };
            string[] words2 = { "Twenty ", "Thirty ", "Forty ", "Fifty ", "Sixty ", "Seventy ", "Eighty ", "Ninety " };
            string[] words3 = { "Thousand ", "Million ", "Billion " };
            num[0] = number % 1000;           // units
            num[1] = number / 1000;
            num[2] = number / 1000000;
            num[1] = num[1] - 1000 * num[2];  // thousands
            num[3] = number / 1000000000;     // billions
            num[2] = num[2] - 1000 * num[3];  // millions
            for (int i = 3; i > 0; i--)
            {
                if (num[i] != 0)
                {
                    first = i;
                    break;
                }
            }
            for (int i = first; i >= 0; i--)
            {
                if (num[i] == 0) continue;
                u = num[i] % 10;              // ones
                t = num[i] / 10;
                h = num[i] / 100;             // hundreds
                t = t - 10 * h;               // tens
                if (h > 0) sb.Append(words0[h] + "Hundred ");
                if (u > 0 || t > 0)
                {
                    if (h > 0 || i < first) sb.Append(and);
                    if (t == 0)
                        sb.Append(words0[u]);
                    else if (t == 1)
                        sb.Append(words1[u]);
                    else
                        sb.Append(words2[t - 2] + words0[u]);
                }
                if (i != 0) sb.Append(words3[i - 1]);
            }
            return sb.ToString().TrimEnd();
        }

        internal static string GetRefNumber()
        {
            var guid = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var refNumber = Regex.Replace(guid, "[a-zA-Z]", string.Empty).Substring(0, 12);

            return refNumber;
        }

        internal static List<Institution> GetInstitutionList(IFMISTZDbContext db, string userId)
        {
            //var institutions = db.Institution.Distinct().OrderBy(a => a.InstitutionCode).ToList();
            //return institutions;

            InstitutionListVm vm = new InstitutionListVm();

            var InstitutionCodeList = (from a in db.Institution
                                       join b in db.InstitutionSubLevels on a.InstitutionId equals b.InstitutionId
                                       join c in db.UserPayStations on b.InstitutionSubLevelId equals c.PayStationId
                                       where c.UserId == userId && c.IsDefault == true && a.OverallStatus.ToUpper() == "ACTIVE"
                                       select a).AsEnumerable().DistinctBy(a => a.InstitutionCode).ToList();
            return InstitutionCodeList;
        }
        internal static List<ConsolidationHeaderSetup> GetGroupInstitutionList(IFMISTZDbContext db, string userId)
        {

            var InstitutionCodeList = (from a in db.ConsolidationHeaderSetups
                                       join b in db.InstitutionSubLevels on a.ConsolidationHeaderCode equals b.InstitutionCode
                                       join c in db.UserPayStations on b.InstitutionSubLevelId equals c.PayStationId
                                       where c.UserId == userId && a.Status == "Active"
                                       select a).AsEnumerable().DistinctBy(a => a.ConsolidationHeaderCode).ToList();
            return InstitutionCodeList;
        }
        internal static List<PaymentVoucher> GetPaidInstitutionList(IFMISTZDbContext db, string userId, DateTime startDate, DateTime endDate)
        {
            var statuses = new[] { "BackLog-Verified", "BackLog-Approved", "approved", "Sent to PO", "Sent to BOT", "generated", "processed", "settled", "accepted" };
            var InstitutionCodeList = (from a in db.PaymentVouchers
                                       join b in db.InstitutionSubLevels on a.InstitutionId equals b.InstitutionId
                                       join c in db.UserPayStations on b.InstitutionSubLevelId equals c.PayStationId
                                       join d in db.ConsolidationDetailSetups on a.InstitutionCode equals d.InstitutionCode
                                       //join e in db.ConsolidationHeaderSetups on d.ConsolidationHeaderSetupId equals e.ConsolidationHeaderSetupId
                                       where c.UserId == userId
                                             && DbFunctions.TruncateTime(a.ApprovedAt) >= DbFunctions.TruncateTime(startDate)
                                             && DbFunctions.TruncateTime(a.ApprovedAt) <= DbFunctions.TruncateTime(endDate)
                                             && statuses.Contains(a.OverallStatus)
                                       select a).AsEnumerable().DistinctBy(a => a.InstitutionCode).ToList();
            return InstitutionCodeList;
        }
        internal static List<PaymentVoucher> GetSourceModuleList(IFMISTZDbContext db, string institutionPicked)
        {
            var sourceModuleList = db.PaymentVouchers
                                        .Where(a => a.InstitutionCode == institutionPicked)
                                        .OrderBy(a => a.SourceModule)
                                        .DistinctBy(a => a.SourceModule)
                                        .ToList();
            return sourceModuleList;
        }

        internal static List<InstitutionAccount> GetAccountList(IFMISTZDbContext db, string userId)
        {
            var institutionAccountList = (from a in db.InstitutionAccounts
                                          join b in db.InstitutionSubLevels on a.InstitutionId equals b.InstitutionId
                                          join c in db.UserPayStations on b.InstitutionSubLevelId equals c.PayStationId
                                          where c.UserId == userId
										  && c.IsDefault == true										  
										  && a.OverallStatus != "Cancelled"
                                          select a).AsEnumerable().DistinctBy(a => a.AccountNumber).ToList();
            return institutionAccountList;

        }

        internal static List<CoaSegment> GetAccountCategory(IFMISTZDbContext db)
        {

            var accountCategoryList = db.CoaSegments
                .Where(c => c.SegmentNo == 7
                            && c.Status.ToUpper() == "ACTIVE"
                            && c.SegmentDesc != "N/A").AsEnumerable().DistinctBy(c => c.SegmentCode).ToList();
            return accountCategoryList;

        }

        internal static List<Currency> GetInstitutionCurrency(IFMISTZDbContext db, string userId)
        {
            var institutionCurrencyList = (from a in db.InstitutionAccounts
                                           join b in db.Currencies on a.Currency equals b.CurrencyCode
                                           join c in db.InstitutionSubLevels on a.InstitutionId equals c.InstitutionId
                                           join d in db.UserPayStations on c.InstitutionSubLevelId equals d.PayStationId
                                           where d.UserId == userId
                                           select b).AsEnumerable().DistinctBy(a => a.CurrencyCode).ToList();
            return institutionCurrencyList;
        }

        internal static List<ReconciliationPosted> GetRecPostedCheckDate(IFMISTZDbContext db)
        {
            InstitutionListVm vm = new InstitutionListVm();
            var PostedReconCheckDate = (from a in db.ReconciliationPosteds
                                        select a).ToList();
            return PostedReconCheckDate;
        }


        //internal static InstitutionSubLevel GetUserPayStation(IFMISTZDbContext db, string userId)
        //{

        //    var payStationId = db.UserPayStations.Where(a => a.UserId == userId && a.IsDefault == true)
        //        .Select(a => a.PayStationId).FirstOrDefault();

        //    var userPayStation = db.InstitutionSubLevels.Find(payStationId);

        //    return userPayStation;
        //}
        internal static InstitutionSubLevel GetUserPayStation(IFMISTZDbContext db, string userId)
        {

            var payStationId = db.UserPayStations.Where(a => a.UserId == userId && a.IsDefault == true)
                .Select(a => a.PayStationId).FirstOrDefault();

            //var userPayStation = db.InstitutionSubLevels.Find(payStationId);

            var userPayStation = db.InstitutionSubLevels.Where(a => a.InstitutionSubLevelId == payStationId
                                                            && a.OverallStatus.ToUpper() == "Active").FirstOrDefault();

            return userPayStation;
        }
        public static DefaultUserPayStationVM GetDefaultUserPayStation(IFMISTZDbContext db, string userId)
        {
            var userPayStation = (from u in db.UserPayStations
                                  join v in db.InstitutionSubLevels on u.PayStationId equals v.InstitutionSubLevelId
                                  join w in db.Institution on v.InstitutionId equals w.InstitutionId
                                  where u.UserId == userId && u.IsDefault == true
                                  select new DefaultUserPayStationVM
                                  {
                                      PayStationId = u.PayStationId,
                                      InstitutionId = w.InstitutionId,
                                      InstitutionCode = w.InstitutionCode,
                                      SubLevelCategory = v.SubLevelCategory,
                                      SubLevelCode = v.SubLevelCode,
                                      SubLevelDesc = v.SubLevelDesc,
                                      VoteCode = w.VoteCode,
                                      InstitutionName = w.InstitutionName,
                                      InstitutionLevel = w.InstitutionLevel,
                                      InstitutionCategory = w.InstitutionCategory,
                                      SpCode = w.SpCode,
                                      SubSpCode = w.SubSpCode,
                                      SpSysId = w.SpSysId
                                  }).FirstOrDefault();

            return userPayStation;
        }

        public static int GetDefaultUserInstitutionId(IFMISTZDbContext db, string userId)
        {
            var userPayStation = (from u in db.UserPayStations
                                  join v in db.InstitutionSubLevels on u.PayStationId equals v.InstitutionSubLevelId
                                  join w in db.Institution on v.InstitutionId equals w.InstitutionId
                                  where u.UserId == userId && u.IsDefault == true
                                  select new
                                  {
                                      InstitutionId = w.InstitutionId
                                  }).FirstOrDefault();

            return Convert.ToInt32(userPayStation);
        }

        public static string GetLegalNumber(IFMISTZDbContext db, string institutionCode, string journalCodePrefix, int id)
        {
            string legalNumber = "";
            try
            {


                int financialYear = (int)GetFinancialYear(db, DateTime.Now);
                int fyPrefix = int.Parse(financialYear.ToString().Substring(2, 2));
                // --  Number, VoteCode, FY, JournalCode

                SequenceNumber sequenceNumber = new SequenceNumber
                {
                    InstitutionCode = institutionCode,
                    JournalCode = journalCodePrefix,
                    Number = id,
                    FY = fyPrefix,
                };

                db.SequenceNumbers.Add(sequenceNumber);
                db.SaveChanges();


                //var parameters = new SqlParameter[] { new SqlParameter("@ID", id), new SqlParameter("@FY", fyPrefix), new SqlParameter("@Vote", institutionCode), new SqlParameter("@JournalCode", journalCodePrefix) };
                //int nextNumber= db.Database.ExecuteSqlCommand("dbo.sp_GenerateSequenceNumber @ID,@FY,@Vote,@JournalCode", parameters);

                int nextNumber = 0;
                int numPreviousEntries = db.SequenceNumbers.Where(a => a.InstitutionCode == institutionCode && a.FY == fyPrefix && a.JournalCode == journalCodePrefix && a.Number < id).ToList().Count();


                //if(numPreviousEntries == 0)
                //{
                //    legalNumber = "Error generating legal number";
                //}
                nextNumber = numPreviousEntries + 1;
                legalNumber = institutionCode + journalCodePrefix + fyPrefix.ToString() + nextNumber.ToString().PadLeft(5, '0');

            }
            catch (Exception ex)
            {
                legalNumber = "Error generating legal number. Details " + ex.Message.ToString();
            }

            return legalNumber;
        }

        public static ProcessResponse GeneratePaymentVoucher(IFMISTZDbContext db, string journalCode, int id, System.Security.Principal.IPrincipal loggedInUser)
        
        {
            ProcessResponse processResponse = new ProcessResponse();
            processResponse.OverallStatus = "Pending";
            // int paymentVoucherId = 0;
            processResponse.ReturnId = 0;

            try
            {
                PaymentVoucher paymentVoucher = new PaymentVoucher();
                if (journalCode == "IM")
                {
                    Imprest imprest = db.Imprests.Find(id);
                    if (imprest == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Imprest Transaction not found";
                        return processResponse;
                    }

                    var payerBank = db.InstitutionAccounts
                        .Where(a => a.SubBudgetClass == imprest.SubBudgetClass
                           && a.InstitutionCode == imprest.InstitutionCode
                           && a.OverallStatus != "Cancelled")
                        .FirstOrDefault();

                    if (payerBank == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Account Setup is Incomplete. There is no expenditure account for sub budget class '" + imprest.SubBudgetClass + "'. Please consult Administrator!";
                        return processResponse;
                    }
                    var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode.ToUpper() == imprest.PayeeType.ToUpper()).FirstOrDefault();

                    if (payeeType == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Vendor setup is incomplete. There is no payee type setup for '" + imprest.PayeeType + "'. Please contact Administrator!";
                        return processResponse;
                    }
                    var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == imprest.SubBudgetClass && a.InstitutionCode == imprest.InstitutionCode).FirstOrDefault();
                    if (crCodes == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + imprest.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return processResponse;
                    }

                    var unappliedAccount = db.InstitutionAccounts
                   .Where(a => a.InstitutionCode == imprest.InstitutionCode
                       && a.AccountType.ToUpper() == "UNAPPLIED"
                       && a.IsTSA == false
                       && a.OverallStatus != "Cancelled"
                   ).FirstOrDefault();

                    if (unappliedAccount == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + imprest.InstitutionName + "'. Please consult Administrator!";
                        return processResponse;
                    }
                    paymentVoucher = new PaymentVoucher
                    {
                        //  PVNo,
                        SourceModule = "Imprest",
                        SourceModuleReferenceNo = imprest.ImprestNo,
                        JournalTypeCode = "PV",//imprest.JournalTypeCode,
                        //,InvoiceNo
                        //,InvoiceDate
                        Narration = imprest.ImprestTypeDesc + " - " + imprest.Description,
                        PaymentDesc = imprest.ImprestTypeDesc,
                        PayeeDetailId = imprest.PayeeDetailId,
                        PayeeCode = imprest.PayeeCode,
                        Payeename = imprest.PayeeName,
                        PayeeBankAccount = imprest.PayeeBankAccount,
                        PayeeBankName = imprest.PayeeBankName,
                        PayeeAccountName = imprest.PayeeAccountName,
                        PayeeAddress = imprest.PayeeAddress,
                        PayeeBIC = imprest.PayeeBIC,
                        PayeeType = imprest.PayeeType,
                        PayerBankAccount = payerBank.AccountNumber,
                        PayerBankName = payerBank.AccountName,
                        PayerBIC = payerBank.BIC,
                        PayerCashAccount = payerBank.GlAccount,
                        PayerAccountType = payerBank.AccountType,
                        OperationalAmount = imprest.OperationalAmount,
                        BaseAmount = imprest.BaseAmount,
                        BaseCurrency = imprest.BaseCurrency,
                        OperationalCurrency = imprest.OperationalCurrency,
                        ExchangeRate = imprest.ExchangeRate,
                        ApplyDate = DateTime.Now,
                        SubBudgetClass = imprest.SubBudgetClass,
                        PaymentMethod = "EFT",
                        FinancialYear = ServiceManager.GetFinancialYear(db, imprest.ApplyDate),
                        CreatedBy = loggedInUser.Identity.Name,
                        CreatedAt = DateTime.Now,
                        OverallStatus = "Pending",
                        Book = "Main",
                        InstitutionId = imprest.InstitutionId,
                        InstitutionCode = imprest.InstitutionCode,
                        InstitutionName = imprest.InstitutionName,
                        PaystationId = imprest.PaystationId,
                        SubLevelCategory = imprest.SubLevelCategory,
                        SubLevelCode = imprest.SubLevelCode,
                        SubLevelDesc = imprest.SubLevelDesc,
                        ReversalFlag = false,
                        GeneralLedgerStatus = "Pending",
                        QueueId = 0,
                        OverallStatusDesc = "Pending",
                        PayableGlAccount = crCodes.CrCoa,
                        UnappliedAccount = unappliedAccount.AccountNumber,
                        InstitutionAccountId = payerBank.InstitutionAccountId,
                        OtherSourceId = id,
                    };

                    if (paymentVoucher == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Error saving payment voucher";
                        return processResponse;
                    }
                    db.PaymentVouchers.Add(paymentVoucher);

                    //db.SaveChanges();

                    //var insertedPaymentVoucher = db.PaymentVouchers.OrderByDescending(a => a.PaymentVoucherId).FirstOrDefault();
                    //if (insertedPaymentVoucher == null)
                    //{
                    //    processResponse.OverallStatus = "Error";
                    //    processResponse.OverallStatusDescription = "Error saving payment voucher";
                    //    return processResponse;
                    //}


                    List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                    List<ImprestDetail> imprestDetailList = db.ImprestDetails.Where(a => a.ImprestId == imprest.ImprestId).ToList();


                    foreach (ImprestDetail imprestDetail in imprestDetailList)
                    {
                        VoucherDetail voucherDetail = new VoucherDetail
                        {
                            PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                            JournalTypeCode = "PV",
                            DrGlAccount = imprestDetail.CrGlAccount,
                            DrGlAccountDesc = imprestDetail.CrGlAccountDesc,
                            CrGlAccount = crCodes.CrCoa,
                            CrGlAccountDesc = crCodes.CrCoaDesc,
                            FundingReferenceNo = imprestDetail.FundingRef,
                            OperationalAmount = imprestDetail.IssuedAmount,
                            BaseAmount = imprestDetail.BaseAmount,
                            //TaxId
                            //TaxCode
                            //TaxName
                            //TaxRate
                        };

                        voucherDetailList.Add(voucherDetail);
                    }
                    db.VoucherDetails.AddRange(voucherDetailList);

                    db.SaveChanges();

                    // PaymentVoucher insertedPaymentVoucher = db.PaymentVouchers.Find(paymentVoucher.PaymentVoucherId);

                    paymentVoucher.PVNo = GetLegalNumber(db, imprest.InstitutionCode, "V", paymentVoucher.PaymentVoucherId);


                    imprest.PVNo = paymentVoucher.PVNo;
                    imprest.PaymentVoucherId = paymentVoucher.PaymentVoucherId;

                    db.SaveChanges();

                    processResponse.ReturnId = paymentVoucher.PaymentVoucherId;
                    processResponse.OverallStatus = "Success";
                }
                else if (journalCode == Libraries.Constants.PREPAYMENT_APPROVE_JOURNAL_TYPE_CODE)
                {
                    PrePayment prePayment = db.PrePayments.Find(id);
                    if (prePayment == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Prepayment Transaction not found";
                        return processResponse;
                    }

                    var payerBank = db.InstitutionAccounts
                        .Where(a => a.SubBudgetClass == prePayment.SubBudgetClass
                        && a.InstitutionCode == prePayment.InstitutionCode
                        && a.OverallStatus != Libraries.Constants.CANCELLED).FirstOrDefault();
                    if (payerBank == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Account Setup is Incomplete. There is no expenditure account for sub budget class '" + prePayment.SubBudgetClass + "'. Please consult Administrator!";
                        return processResponse;
                    }
                    var payeeType = db.PayeeTypes
                        .Where(a => a.PayeeTypeCode.ToUpper() == prePayment.PayeeType.ToUpper()
                        && a.Status != Libraries.Constants.CANCELLED).FirstOrDefault();

                    if (payeeType == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Vendor setup is incomplete. There is no payee type setup for '" + prePayment.PayeeType + "'. Please contact Administrator!";
                        return processResponse;
                    }
                    var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == prePayment.SubBudgetClass && a.InstitutionCode == prePayment.InstitutionCode).FirstOrDefault();
                    //var crCodes = db.JournalTypeViews.Where(a => a.JournalTypeCode == "PV" && a.JournalTypeDesc == "Prepayment Payment voucher"  && a.TrxType == "CR" && a.SubBudgetClass == prePayment.SubBudgetClass  && a.InstitutionCode == prePayment.InstitutionCode).FirstOrDefault();
                    if (crCodes == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Chart of Account setup is incomplete for Credit Account. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + prePayment.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return processResponse;
                    }
                    // var drCodes = db.JournalTypeViews.Where(a => a.DrGfsCode == payeeType.GfsCode && a.SubBudgetClass == prePayment.SubBudgetClass && a.JournalTypeCode == Libraries.Constants.PREPAYMENT_APPROVE_JOURNAL_TYPE_CODE &&  a.InstitutionCode == prePayment.InstitutionCode).FirstOrDefault();
                    var drCodes = db.JournalTypeViews.Where(a => a.JournalTypeCode == "PV" && a.JournalTypeDesc == "Prepayment Payment voucher" && a.TrxType == "DR" && a.InstitutionCode == prePayment.InstitutionCode).FirstOrDefault();
                    if (drCodes == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Chart of Account setup is incomplete for Debit Account. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + prePayment.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return processResponse;
                    }

                    var unappliedAccount = db.InstitutionAccounts
                   .Where(a => a.InstitutionCode == prePayment.InstitutionCode
                   && a.AccountType.ToUpper() == "UNAPPLIED"
                   && a.IsTSA == false
                   && a.OverallStatus != "Cancelled"
                   ).FirstOrDefault();

                    if (unappliedAccount == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + prePayment.InstitutionName + "'. Please consult Administrator!";
                        return processResponse;
                    }

                    decimal? OperationalAmount = 0;

                    if (Properties.Settings.Default.HostingEnvironment == "Dev")
                    {
                        OperationalAmount = prePayment.PrePaymentAmount;
                    }
                    else
                    {
                        OperationalAmount = prePayment.OperationalAmount;
                    }
                    string paymentDesc = null;
                    if (prePayment.PrePaymentDesc.Length > 80)
                    {
                        paymentDesc = prePayment.PrePaymentDesc.Substring(0, 80);
                    }
                    else
                    {
                        paymentDesc = prePayment.PrePaymentDesc;
                    }
                    paymentVoucher = new PaymentVoucher
                    {
                        //  PVNo,
                        SourceModule = "Prepayment",
                        SourceModuleReferenceNo = prePayment.PrePaymentNo,
                        JournalTypeCode = "PV",
                        Narration = prePayment.PrePaymentDesc,
                        PaymentDesc = paymentDesc,
                        PayeeDetailId = prePayment.PayeeDetailId,
                        PayeeCode = prePayment.PayeeCode,
                        Payeename = prePayment.Payeename,
                        PayeeBankAccount = prePayment.PayeeBankAccount,
                        PayeeBankName = prePayment.PayeeBankName,
                        PayeeAccountName = prePayment.PayeeAccountName,
                        PayeeAddress = prePayment.PayeeAddress,
                        PayeeBIC = prePayment.PayeeBIC,
                        PayeeType = prePayment.PayeeType,
                        PayerBankAccount = payerBank.AccountNumber,
                        PayerBankName = payerBank.AccountName,
                        PayerBIC = payerBank.BIC,
                        PayerCashAccount = payerBank.GlAccount,
                        PayerAccountType = payerBank.AccountType,
                        OperationalAmount = OperationalAmount,
                        BaseAmount = prePayment.BaseAmount,
                        BaseCurrency = prePayment.BaseCurrency,
                        OperationalCurrency = prePayment.OperationalCurrency,
                        ExchangeRate = prePayment.CurrentExchangeRate,
                        ApplyDate = DateTime.Now,
                        SubBudgetClass = prePayment.SubBudgetClass,
                        PaymentMethod = "EFT",
                        FinancialYear = ServiceManager.GetFinancialYear(db, (DateTime)prePayment.ApplyDate),
                        CreatedBy = loggedInUser.Identity.Name,
                        CreatedAt = DateTime.Now,
                        OverallStatus = Libraries.Constants.PENDING,
                        Book = "Main",
                        InstitutionId = prePayment.InstitutionId,
                        InstitutionCode = prePayment.InstitutionCode,
                        InstitutionName = prePayment.InstitutionName,
                        PaystationId = prePayment.PaystationId,
                        SubLevelCategory = prePayment.SubLevelCategory,
                        SubLevelCode = prePayment.SubLevelCode,
                        SubLevelDesc = prePayment.SubLevelDesc,
                        ReversalFlag = false,
                        GeneralLedgerStatus = Libraries.Constants.PENDING,
                        QueueId = 0,
                        OverallStatusDesc = Libraries.Constants.PENDING,
                        PayableGlAccount = crCodes.CrCoa,
                        UnappliedAccount = unappliedAccount.AccountNumber,
                        InstitutionAccountId = payerBank.InstitutionAccountId,
                        OtherSourceId = id,

                    };

                    if (paymentVoucher == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Error saving payment voucher";
                        return processResponse;
                    }
                    db.PaymentVouchers.Add(paymentVoucher);

                    List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                    List<PrePaymentDetail> prepaymentDetailList = db.PrePaymentDetails.Where(a => a.PrePaymentId == prePayment.PrePaymentId).ToList();

                    foreach (PrePaymentDetail prepaymentDetail in prepaymentDetailList)
                    {
                        VoucherDetail voucherDetail = new VoucherDetail
                        {
                            PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                            JournalTypeCode = "PV",
                            DrGlAccount = drCodes.DrCoa,
                            DrGlAccountDesc = drCodes.DrCoaDesc,
                            CrGlAccount = crCodes.CrCoa,
                            CrGlAccountDesc = crCodes.CrCoaDesc,
                            FundingReferenceNo = prepaymentDetail.FundingReferenceNo,
                            OperationalAmount = prepaymentDetail.TotalAmount,
                            BaseAmount = prepaymentDetail.TotalAmount,
                            //TaxId
                            //TaxCode
                            //TaxName
                            //TaxRate
                        };


                        voucherDetailList.Add(voucherDetail);
                    }
                    db.VoucherDetails.AddRange(voucherDetailList);

                    db.SaveChanges();

                    paymentVoucher.PVNo = GetLegalNumber(db, prePayment.InstitutionCode, "V", paymentVoucher.PaymentVoucherId);

                    prePayment.PVNo = paymentVoucher.PVNo;
                    prePayment.PaymentVoucherId = paymentVoucher.PaymentVoucherId;

                    db.SaveChanges();

                    processResponse.ReturnId = paymentVoucher.PaymentVoucherId;
                    processResponse.OverallStatus = "Success";
                }

                else if (journalCode == Libraries.Constants.LOAN_APPROVE_JOURNAL_TYPE_CODE)
                {
                    LoanBeneficiary loanBeneficiary = db.LoanBeneficiaries.Find(id);
                    if (loanBeneficiary == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Beneficiary Details not found!";
                        return processResponse;
                     
                    }

                    Loan loan = db.Loans.Find(loanBeneficiary.LoanId);
                    if (loan == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Loan Transaction not found";
                        return processResponse;
                    }

                    var LoanTypeId = loan.LoanTypeId;

                    LoanType loanType = db.LoanTypes.Find(LoanTypeId);

           

                    var payerBank = db.InstitutionAccounts.Where(a => a.SubBudgetClass == loan.SubBudgetClass && a.InstitutionCode == loan.InstitutionCode).FirstOrDefault();
                    if (payerBank == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Account Setup is Incomplete. There is no expenditure account for sub budget class '" + loan.SubBudgetClass + "'. Please consult Administrator!";
                        return processResponse;
                    }
                    var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode.ToUpper() == loan.PayeeType.ToUpper()).FirstOrDefault();

                    if (payeeType == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Vendor setup is incomplete. There is no payee type setup for '" + loan.PayeeType + "'. Please contact Administrator!";
                        return processResponse;
                    }


                    var drCodes = db.JournalTypeViews.Where(a => a.DrGfsCode == payeeType.GfsCode && a.SubBudgetClass == loan.SubBudgetClass && a.InstitutionCode == loan.InstitutionCode).FirstOrDefault();
                    if (drCodes == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. GFS Code with Loan Type '" + loanType.LoanCode + "' does not Exists. Please contact Administrator!";
                        return processResponse;
                    }

                    var crCodes = db.JournalTypeViews.Where(a => a.JournalTypeCode == Libraries.Constants.LOAN_APPROVE_JOURNAL_TYPE_CODE && a.TrxType == "DR" && a.InstitutionCode == loan.InstitutionCode).FirstOrDefault();
                    if (crCodes == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. GFS Code with Loan Type '" + loanType.LoanCode + "' does not Exists. Please contact Administrator!";
                        return processResponse;
                    }

                    var unappliedAccount = db.InstitutionAccounts
                      .Where(a => a.InstitutionCode == loan.InstitutionCode
                        && a.AccountType.ToUpper() == "UNAPPLIED"
                        && a.IsTSA == false
                        && a.OverallStatus != "Cancelled"
                       ).FirstOrDefault();

                    if (unappliedAccount == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + loan.InstitutionName + "'. Please consult Administrator!";
                        return processResponse;
                    }
                    paymentVoucher = new PaymentVoucher
                    {
                        //  PVNo,
                        SourceModule = "Loan",
                        SourceModuleReferenceNo = loan.LoanNo,
                        JournalTypeCode = "PV",
                        Narration = loan.Description,
                        PaymentDesc = loan.Description,
                        PayeeDetailId = loan.PayeeDetailId, // TODO: Use loanBeneficiary.PayeeDetailId
                        PayeeCode = loanBeneficiary.PayeeCode,
                        Payeename = loanBeneficiary.PayeeName,
                        PayeeBankAccount = loanBeneficiary.Accountnumber,
                        PayeeBankName = loanBeneficiary.BankName,
                        PayeeAccountName = loanBeneficiary.AccountName,
                        PayeeAddress = loanBeneficiary.Address,
                        PayeeBIC = loanBeneficiary.BIC,
                        PayeeType = loanBeneficiary.PayeeType,
                        PayerBankAccount = payerBank.AccountNumber,
                        PayerBankName = payerBank.AccountName,
                        PayerBIC = payerBank.BIC,
                        PayerCashAccount = payerBank.GlAccount,
                        PayerAccountType = payerBank.AccountType,
                        OperationalAmount = loanBeneficiary.OperationalAmount,
                        BaseAmount = loanBeneficiary.BaseAmount,
                        BaseCurrency = loan.BaseCurrency,
                        OperationalCurrency = loan.OperationalCurrency,
                        ExchangeRate = loan.ExchangeRate,
                        ApplyDate = DateTime.Now,
                        SubBudgetClass = loan.SubBudgetClass,
                        PaymentMethod = "EFT",
                        FinancialYear = ServiceManager.GetFinancialYear(db, (DateTime)loan.ApplyDate),
                        CreatedBy = loggedInUser.Identity.Name,
                        CreatedAt = DateTime.Now,
                        OverallStatus = Libraries.Constants.PENDING,
                        Book = "Main",
                        InstitutionId = loan.InstitutionId,
                        InstitutionCode = loan.InstitutionCode,
                        InstitutionName = loan.InstitutionName,
                        PaystationId = loan.PaystationId,
                        SubLevelCategory = loan.SubLevelCategory,
                        SubLevelCode = loan.SubLevelCode,
                        SubLevelDesc = loan.SubLevelDesc,
                        ReversalFlag = false,
                        GeneralLedgerStatus = Libraries.Constants.PENDING,
                        QueueId = 0,
                        OverallStatusDesc = Libraries.Constants.PENDING,
                        PayableGlAccount = drCodes.DrCoa, //crCodes.CrCoa,
                        UnappliedAccount = unappliedAccount.AccountNumber,
                        InstitutionAccountId = payerBank.InstitutionAccountId,
                        OtherSourceId = id,
                    };

                    if (paymentVoucher == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Error saving payment voucher";
                        return processResponse;
                    }
                    db.PaymentVouchers.Add(paymentVoucher);

                    List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                    List<Loan> loanDetailList = db.Loans.Where(a => a.LoanId == loan.LoanId).ToList();


                    foreach (Loan loanDetail in loanDetailList)
                    {
                        VoucherDetail voucherDetail = new VoucherDetail
                        {
                            PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                            JournalTypeCode = "PV",
                            DrGlAccount = loan.CrGLAccount, 
                            DrGlAccountDesc = loan.CrGLAccountDesc, 
                            CrGlAccount = drCodes.DrCoa,
                            CrGlAccountDesc = drCodes.DrCoaDesc,
                            OperationalAmount = loanBeneficiary.OperationalAmount,
                            BaseAmount = loanBeneficiary.BaseAmount,                     
                        };

                        voucherDetailList.Add(voucherDetail);
                    }
                    db.VoucherDetails.AddRange(voucherDetailList);

                    db.SaveChanges();

                    paymentVoucher.PVNo = GetLegalNumber(db, loan.InstitutionCode, "V", paymentVoucher.PaymentVoucherId);

                    loan.PVNo = paymentVoucher.PVNo;
                    loan.PaymentVoucherId = paymentVoucher.PaymentVoucherId;

                    db.SaveChanges();

                    processResponse.ReturnId = paymentVoucher.PaymentVoucherId;
                    processResponse.OverallStatus = "Success";
                }
                else if (journalCode == "PO")
                {

                    ReceivingSummaryView receivingSummaryView = db.ReceivingSummaryViews.Where(a => a.ReceivingSummaryId == id).FirstOrDefault();
                    List<ReceivingDetailView> receivingDetailViewList = db.ReceivingDetailViews.Where(a => a.ReceivingSummaryId == receivingSummaryView.ReceivingSummaryId).ToList();
                    if (receivingDetailViewList.Count() > 0)
                    {

                        var lpo = db.PurchaseOrders.Find(receivingSummaryView.PurchaseOrderId);
                        if (receivingSummaryView == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Purchase Order Transaction not found";
                            return processResponse;
                        }
                        var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode.ToUpper() == receivingSummaryView.PayeeType.ToUpper() && a.Status.ToUpper() != "CANCELLED").FirstOrDefault();

                        if (payeeType == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Vendor setup is incomplete. There is no payee type setup for '" + receivingSummaryView.PayeeType + "'. Please contact Administrator!";
                            return processResponse;
                        }
                        var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == receivingSummaryView.SubBudgetClass && a.InstitutionCode == receivingSummaryView.InstitutionCode).FirstOrDefault();
                        if (crCodes == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + receivingSummaryView.SubBudgetClass + "' is missing. Please contact Administrator!";
                            return processResponse;
                        }


                        var unappliedAccount = db.InstitutionAccounts
                          .Where(a => a.InstitutionCode == receivingSummaryView.InstitutionCode
                           && a.AccountType.ToUpper() == "UNAPPLIED"
                           && a.IsTSA == false
                           && a.OverallStatus != "Cancelled"
                          ).FirstOrDefault();

                        if (unappliedAccount == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + receivingSummaryView.InstitutionName + "'. Please consult Administrator!";
                            return processResponse;
                        }
                        var payerBank = db.InstitutionAccounts.Where(a => a.SubBudgetClass == receivingSummaryView.SubBudgetClass && a.InstitutionCode == receivingSummaryView.InstitutionCode && a.OverallStatus.ToUpper() != "CANCELLED").FirstOrDefault();
                        if (payerBank == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Institution Account Setup is Incomplete. There is no expenditure account for sub budget class '" + receivingSummaryView.SubBudgetClass + "'. Please consult Administrator!";
                            return processResponse;
                        }
                        string paymentDesc = null;
                        if (receivingSummaryView.PurchaseOrderDesc.Length > 80)
                        {
                             paymentDesc = receivingSummaryView.PurchaseOrderDesc.Substring(0, 80);
                        }
                        else
                        {
                             paymentDesc = receivingSummaryView.PurchaseOrderDesc;
                        }
                        paymentVoucher = new PaymentVoucher
                        {
                            //  PVNo,
                            SourceModule = "Purchase",
                            SourceModuleReferenceNo = receivingSummaryView.PurchaseOrderNo,
                            JournalTypeCode = "PV",//receivingSummaryView.JournalTypeCode,
                            InvoiceNo = receivingSummaryView.InvoiceNo,
                            InvoiceDate = receivingSummaryView.InvoiceDate,
                            Narration = receivingSummaryView.PurchaseOrderDesc,
                            PaymentDesc = paymentDesc,
                            PayeeDetailId = receivingSummaryView.PayeeDetailId,
                            PayeeCode = receivingSummaryView.PayeeCode,
                            Payeename = lpo.Payeename,
                            PayeeBankAccount = receivingSummaryView.PayeeBankAccount,
                            PayeeBankName = receivingSummaryView.PayeeBankName,
                            PayeeAccountName = lpo.PayeeAccountName,
                            PayeeAddress = receivingSummaryView.PayeeAddress,
                            PayeeBIC = receivingSummaryView.PayeeBIC,
                            PayeeType = receivingSummaryView.PayeeType,
                            PayerBankAccount = receivingSummaryView.payerBankAccountNumber,
                            PayerBankName = receivingSummaryView.payerBankAccountName,
                            PayerBIC = receivingSummaryView.payerBankBIC,
                            PayerCashAccount = receivingSummaryView.payerBankGlAccount,
                            PayerAccountType = payerBank.AccountType,
                            OperationalAmount = receivingSummaryView.ReceivedAmount,
                            BaseAmount = receivingSummaryView.BaseAmount,
                            BaseCurrency = receivingSummaryView.BaseCurrency,
                            OperationalCurrency = receivingSummaryView.OperationalCurrency,
                            ExchangeRate = receivingSummaryView.ExchangeRate,
                            ApplyDate = DateTime.Now,
                            SubBudgetClass = receivingSummaryView.SubBudgetClass,
                            PaymentMethod = "EFT",
                            FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                            CreatedBy = loggedInUser.Identity.Name,
                            CreatedAt = DateTime.Now,
                            OverallStatus = "Pending",
                            Book = "Main",
                            InstitutionId = receivingSummaryView.InstitutionId,
                            InstitutionCode = receivingSummaryView.InstitutionCode,
                            InstitutionName = receivingSummaryView.InstitutionName,
                            PaystationId = receivingSummaryView.PaystationId,
                            SubLevelCategory = receivingSummaryView.SubLevelCategory,
                            SubLevelCode = receivingSummaryView.SubLevelCode,
                            SubLevelDesc = receivingSummaryView.SubLevelDesc,
                            ReversalFlag = false,
                            GeneralLedgerStatus = "Pending",
                            QueueId = 0,
                            OverallStatusDesc = "Pending",
                            PayableGlAccount = crCodes.CrCoa,
                            UnappliedAccount = unappliedAccount.AccountNumber,
                            InstitutionAccountId = payerBank.InstitutionAccountId,
                            OtherSourceId = id,
                        };

                        if (lpo.ShortDesc.ToUpper() == "WORKS")
                        {
                            var receiving_summary = db.ReceivingSummarys.Find(receivingSummaryView.ReceivingSummaryId);
                            decimal payableAmount = (decimal)receiving_summary.ReceivedAmount;
                            if (receiving_summary.VAT > 0)
                            {
                                payableAmount = payableAmount - (decimal)receiving_summary.VAT;
                            }
                            decimal serviceAmount = 2 * payableAmount / 5;
                            decimal goodsAmount = 3 * payableAmount / 5;
                            paymentVoucher.ServiceAmount = serviceAmount;
                            paymentVoucher.VATOnService = receiving_summary.VAT;
                            paymentVoucher.GoodsAmount = goodsAmount;
                            paymentVoucher.VATOnGoods = receiving_summary.VAT;
                            paymentVoucher.OperationalWithHoldingAmount = (serviceAmount * (decimal)0.05) + (goodsAmount * (decimal)0.02);
                            paymentVoucher.BaseWithHoldingAmount = lpo.CurrentExchangeRate * paymentVoucher.OperationalWithHoldingAmount;
                            paymentVoucher.hasWithHolding = true;
                            paymentVoucher.OverallStatusDesc = "WORKS";
                        }

                        db.PaymentVouchers.Add(paymentVoucher);

                        //db.SaveChanges();

                        //var insertedPaymentVoucher = db.PaymentVouchers.OrderByDescending(a => a.PaymentVoucherId).FirstOrDefault();
                        //if (insertedPaymentVoucher == null)
                        //{
                        //    processResponse.OverallStatus = "Error";
                        //    processResponse.OverallStatusDescription = "Error saving payment voucher";
                        //    return processResponse;
                        //}

                        //paymentVoucherId = insertedPaymentVoucher.PaymentVoucherId;
                        //processResponse.ReturnId = paymentVoucherId;
                        //insertedPaymentVoucher.PVNo = ServiceManager.GetLegalNumber(db, receivingSummaryView.InstitutionCode, "V", paymentVoucherId);

                        List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();



                        foreach (ReceivingDetailView receivingDetailView in receivingDetailViewList)
                        {
                            VoucherDetail voucherDetail = new VoucherDetail
                            {
                                PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                                JournalTypeCode = "PV",
                                DrGlAccount = receivingDetailView.GlAccount,
                                DrGlAccountDesc = receivingDetailView.GlAccountDesc,
                                CrGlAccount = crCodes.CrCoa,
                                CrGlAccountDesc = crCodes.CrCoaDesc,
                                FundingReferenceNo = receivingDetailView.FundingReference,
                                OperationalAmount = receivingDetailView.Amount,
                                BaseAmount = receivingDetailView.BaseAmount,
                                //TaxId
                                //TaxCode
                                //TaxName
                                //TaxRate
                            };

                            voucherDetailList.Add(voucherDetail);
                        }
                        db.VoucherDetails.AddRange(voucherDetailList);

                        db.SaveChanges();

                        paymentVoucher.PVNo = GetLegalNumber(db, receivingSummaryView.InstitutionCode, "V", paymentVoucher.PaymentVoucherId);
                       // paymentVoucher.ApplyDate = paymentVoucher.CreatedAt;
                        //purchaseOrder.PVNo = insertedPaymentVoucher.PVNo;
                        //purchaseOrder.PaymentVoucherId = insertedPaymentVoucher.PaymentVoucherId;

                        processResponse.StringReturnValue = paymentVoucher.PVNo;
                        db.SaveChanges();
                        processResponse.ReturnId = paymentVoucher.PaymentVoucherId;
                        processResponse.OverallStatus = "Success";
                    }
                    else
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Receiving Details not found";
                        return processResponse;
                    }
                }
                else if (journalCode == "CO")////
                {
                    ReceivingSummary receivingSummary = db.ReceivingSummarys.Where(a => a.ReceivingSummaryId == id).FirstOrDefault();
                    List<ReceivingContractDetailView> receivingContractDetailViewList = db.ReceivingContractDetailViews.Where(a => a.ReceivingSummaryId == receivingSummary.ReceivingSummaryId).ToList();
                    if (receivingContractDetailViewList.Count() > 0)
                    {
                        if (receivingSummary == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Purchase Order Transaction not found";
                            return processResponse;
                        }

                        Contract contract = db.Contracts.Find(receivingSummary.ContractId);
                        if (contract == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Contract Details not found";
                            return processResponse;
                        }

                        //var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode.ToUpper() == contract.PayeeType.ToUpper()).FirstOrDefault();
                        var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode.ToUpper() == "SUPPLIER" && a.Status.ToUpper() != "CANCELLED").FirstOrDefault();
                        if (payeeType == null)
                        {
                            processResponse.OverallStatus = "Error";
                            //processResponse.OverallStatusDescription = "Vendor setup is incomplete. There is no payee type setup for '" + receivingSummary.PayeeType + "'. Please contact Administrator!";
                            processResponse.OverallStatusDescription = "Vendor setup is incomplete. There is no payee type setup for '" + " Supplier" + "'. Please contact Administrator!";
                            return processResponse;
                        }
                        var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == contract.SubBudgetClass && a.InstitutionCode == contract.InstitutionCode).FirstOrDefault();
                        if (crCodes == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + contract.SubBudgetClass + "' is missing. Please contact Administrator!";
                            return processResponse;
                        }


                        var unappliedAccount = db.InstitutionAccounts
                       .Where(a => a.InstitutionCode == receivingSummary.InstitutionCode
                       && a.AccountType.ToUpper() == "UNAPPLIED"
                       && a.IsTSA == false
                       && a.OverallStatus != "Cancelled"
                       ).FirstOrDefault();

                        if (unappliedAccount == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + contract.InstitutionName + "'. Please consult Administrator!";
                            return processResponse;
                        }
                        PaymentSchedule paymentSchedule = new PaymentSchedule();
                        if (receivingSummary.Type != "AdvancePayment")
                        {
                             paymentSchedule = db.PaymentSchedules.Find(receivingSummary.PaymentScheduleId);
                            if (paymentSchedule == null)
                            {
                                processResponse.OverallStatus = "Error";
                                processResponse.OverallStatusDescription = "Payment Schedule for the contract not found. Please consult Administrator!";
                                return processResponse;
                            }
                        }
                        var payerBank = db.InstitutionAccounts.Where(a => a.SubBudgetClass == contract.SubBudgetClass && a.InstitutionCode == contract.InstitutionCode && a.OverallStatus.ToUpper() != "CANCELLED").FirstOrDefault();
                        if (payerBank == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "Institution Account Setup is Incomplete. There is no expenditure account for sub budget class '" + contract.SubBudgetClass + "'. Please consult Administrator!";
                            return processResponse;
                        }

                        if (receivingSummary.HasRetention)
                        {
                            var bank_account_from = db.InstitutionAccounts.Where(a => a.InstitutionCode == contract.InstitutionCode && a.SubBudgetClass == contract.SubBudgetClass && a.OverallStatus.ToUpper() != "CANCELLED").Select(a => a.AccountNumber).FirstOrDefault();

                            string cashAccountDr = db.InstitutionAccounts.Where(a => a.AccountNumber == receivingSummary.BankAccountTo & a.InstitutionCode == receivingSummary.InstitutionCodeTo && a.SubBudgetClass == receivingSummary.SubBudgetClassTo && a.OverallStatus != "Cancelled").Select(a => a.GlAccount).FirstOrDefault();
                            string cashAccountCr = db.InstitutionAccounts.Where(a => a.AccountNumber == bank_account_from & a.InstitutionCode == contract.InstitutionCode && a.SubBudgetClass == contract.SubBudgetClass && a.OverallStatus != "Cancelled").Select(a => a.GlAccount).FirstOrDefault();
                            if (cashAccountDr == null)
                            {
                                processResponse.OverallStatus = "Error";
                                processResponse.OverallStatusDescription = "Transfer Retention,Debit Account Setup for " + receivingSummary.InstitutionCodeTo + " Does not Exist, Contact System Administrator ";
                                return processResponse;
                            }
                            else if (cashAccountDr == null)
                            {
                                processResponse.OverallStatus = "Error";
                                processResponse.OverallStatusDescription = "Transfer Retention,Credit Account Setup for " + contract.InstitutionCode + " Does not Exist, Contact System Administrator ";
                                return processResponse;
                            }
                            else
                            {
                                string paymentDesc = null;
                                string narration = null;
                                if (receivingSummary.Type != "AdvancePayment")
                                {
                                    if (paymentSchedule.Description.Length > 80)
                                    {
                                        paymentDesc = paymentSchedule.Description.Substring(0, 80);
                                    }
                                    else
                                    {
                                        paymentDesc = paymentSchedule.Description;
                                    }
                                    narration = paymentSchedule.Description;
                                }
                                else
                                {
                                    paymentDesc = "Advance Payment";
                                    narration = "Advance Payment";
                                }
                                paymentVoucher = new PaymentVoucher
                                {
                                    //  PVNo,
                                    SourceModule = "Contract",
                                    SourceModuleReferenceNo = contract.ContractNo,
                                    JournalTypeCode = "PV",//receivingSummaryView.JournalTypeCode,
                                    InvoiceNo = receivingSummary.InvoiceNo,
                                    InvoiceDate = receivingSummary.InvoiceDate,
                                    Narration = narration,
                                    PaymentDesc = paymentDesc,
                                    PayeeDetailId = contract.PayeeDetailId,
                                    PayeeCode = contract.PayeeCode,
                                    Payeename = contract.Payeename,
                                    PayeeAccountName = contract.PayeeAccountName,
                                    PayeeBankAccount = contract.PayeeBankAccount,
                                    PayeeBankName = contract.PayeeBankName,                      
                                    PayeeAddress = contract.PayeeAddress,
                                    PayeeBIC = contract.PayeeBIC,
                                    PayeeType = contract.PayeeType,
                                    PayerBankAccount = payerBank.AccountNumber,
                                    PayerBankName = payerBank.AccountName,
                                    PayerBIC = payerBank.BIC,
                                    PayerCashAccount = payerBank.ReceivingGlAccount,
                                    PayerAccountType = payerBank.AccountType,
                                    OperationalAmount = receivingSummary.ReceivedAmount,
                                    BaseAmount = receivingSummary.ReceivedAmount,
                                    BaseCurrency = contract.BaseCurrency,
                                    OperationalCurrency = contract.OperationalCurrency,
                                    ExchangeRate = 1,
                                    ApplyDate = DateTime.Now,
                                    SubBudgetClass = contract.SubBudgetClass,
                                    PaymentMethod = "EFT",
                                    FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                                    CreatedBy = loggedInUser.Identity.Name,
                                    CreatedAt = DateTime.Now,
                                    OverallStatus = "Pending",
                                    Book = "Main",
                                    InstitutionId = contract.InstitutionId,
                                    InstitutionCode = contract.InstitutionCode,
                                    InstitutionName = contract.InstitutionName,
                                    PaystationId = contract.PaystationId,
                                    SubLevelCategory = contract.SubLevelCategory,
                                    SubLevelCode = contract.SubLevelCode,
                                    SubLevelDesc = contract.SubLevelDesc,
                                    ReversalFlag = false,
                                    GeneralLedgerStatus = "Pending",
                                    QueueId = 0,
                                    OverallStatusDesc = "Pending",
                                    PayableGlAccount = crCodes.CrCoa,
                                    UnappliedAccount = unappliedAccount.AccountNumber,
                                    InstitutionAccountId = payerBank.InstitutionAccountId,
                                    OtherSourceId = id,
                                    RetentionAmount = receivingSummary.ReceivedAmount * receivingSummary.RetentionPercentage / 100,
                                    TransfertType = receivingSummary.RetentionBy,
                                    SubBudgetClassTo = receivingSummary.SubBudgetClassTo,
                                    BankAccountTo = receivingSummary.BankAccountTo,
                                    AdvancePayment = receivingSummary.AdvancePayment
                                };

                            }
                        }
                        else
                        {
                            string paymentDesc = null;
                            string narration = null;
                            string soureceModule = null;
                            if (receivingSummary.Type != "AdvancePayment")
                            {
                                if (paymentSchedule.Description.Length > 80)
                                {
                                    paymentDesc = paymentSchedule.Description.Substring(0, 80);
                                }
                                else
                                {
                                    paymentDesc = paymentSchedule.Description;
                                }
                                narration = paymentSchedule.Description;
                                soureceModule = "Contract";
                            }
                            else
                            {
                                paymentDesc = "Advance Payment";
                                narration = receivingSummary.PaymentDescription;
                                soureceModule = "Advance Payment";
                            }
                            paymentVoucher = new PaymentVoucher
                            {
                                //  PVNo,
                                SourceModule = soureceModule,
                                SourceModuleReferenceNo = contract.ContractNo,
                                JournalTypeCode = "PV",//receivingSummaryView.JournalTypeCode,
                                InvoiceNo = receivingSummary.InvoiceNo,
                                InvoiceDate = receivingSummary.InvoiceDate,
                                Narration = narration,
                                PaymentDesc = paymentDesc,
                                PayeeDetailId = contract.PayeeDetailId,
                                PayeeCode = contract.PayeeCode,
                                Payeename = contract.Payeename,
                                PayeeBankName = contract.PayeeBankName,
                                PayeeAccountName = contract.PayeeAccountName,
                                PayeeBankAccount = contract.PayeeBankAccount,                                
                                PayeeAddress = contract.PayeeAddress,
                                PayeeBIC = contract.PayeeBIC,
                                PayeeType = contract.PayeeType,
                                PayerBankAccount = payerBank.AccountNumber,
                                PayerBankName = payerBank.AccountName,
                                PayerBIC = payerBank.BIC,
                                PayerCashAccount = payerBank.ReceivingGlAccount,
                                PayerAccountType = payerBank.AccountType,
                                OperationalAmount = receivingSummary.ReceivedAmount,
                                BaseAmount = receivingSummary.ReceivedAmount,
                                BaseCurrency = contract.BaseCurrency,
                                OperationalCurrency = contract.OperationalCurrency,
                                //ExchangeRate = contract.ExchangeRate,
                                ExchangeRate = 1,
                                ApplyDate = DateTime.Now,
                                SubBudgetClass = contract.SubBudgetClass,
                                PaymentMethod = "EFT",
                                FinancialYear = ServiceManager.GetFinancialYear(db, DateTime.Now),
                                CreatedBy = loggedInUser.Identity.Name,
                                CreatedAt = DateTime.Now,
                                OverallStatus = "Pending",
                                Book = "Main",
                                InstitutionId = contract.InstitutionId,
                                InstitutionCode = contract.InstitutionCode,
                                InstitutionName = contract.InstitutionName,
                                PaystationId = contract.PaystationId,
                                SubLevelCategory = contract.SubLevelCategory,
                                SubLevelCode = contract.SubLevelCode,
                                SubLevelDesc = contract.SubLevelDesc,
                                ReversalFlag = false,
                                GeneralLedgerStatus = "Pending",
                                QueueId = 0,
                                OverallStatusDesc = "Pending",
                                PayableGlAccount = crCodes.CrCoa,
                                UnappliedAccount = unappliedAccount.AccountNumber,
                                InstitutionAccountId = payerBank.InstitutionAccountId,
                                OtherSourceId = id,
                                AdvancePayment = receivingSummary.AdvancePayment
                            };

                        }

                        if (contract.ContractType.ToUpper() == "WORKS")
                        {
                            if (receivingSummary.Type != "AdvancePayment")
                            {
                                decimal payableAmount = (decimal)receivingSummary.ReceivedAmount;
                            if (receivingSummary.VAT > 0)
                            {
                                payableAmount = payableAmount - (decimal)receivingSummary.VAT;
                            }
                            decimal serviceAmount = 2 * payableAmount / 5;
                            decimal goodsAmount = 3 * payableAmount / 5;
                            paymentVoucher.ServiceAmount = serviceAmount;
                            paymentVoucher.VATOnService = receivingSummary.VAT;
                            paymentVoucher.GoodsAmount = goodsAmount;
                            paymentVoucher.VATOnGoods = receivingSummary.VAT;
                            paymentVoucher.OperationalWithHoldingAmount = (serviceAmount * (decimal)0.05) + (goodsAmount * (decimal)0.02);
                            paymentVoucher.BaseWithHoldingAmount = contract.CurrentExchangeRate * paymentVoucher.OperationalWithHoldingAmount;
                            paymentVoucher.hasWithHolding = true;
                            paymentVoucher.OverallStatusDesc = "WORKS";
                           }
                        }

                        if (receivingSummary.HasLiquidatedDamage)
                        {
                            paymentVoucher.LiquidatedDemageAmount = receivingSummary.LiquidatedDamageAmount;
                        }


                        db.PaymentVouchers.Add(paymentVoucher);


                        List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                        if (receivingContractDetailViewList == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription = "No valid receiving detail with the COA items found. Please consult Administrator!";
                            return processResponse;
                        }
                        if (receivingSummary.HasRetention)
                        {
                            var bank_account_from = db.InstitutionAccounts.Where(a => a.InstitutionCode == contract.InstitutionCode && a.SubBudgetClass == contract.SubBudgetClass).Select(a => a.AccountNumber).FirstOrDefault();

                            string cashAccountDr = db.InstitutionAccounts.Where(a => a.AccountNumber == receivingSummary.BankAccountTo & a.InstitutionCode == receivingSummary.InstitutionCodeTo && a.SubBudgetClass == receivingSummary.SubBudgetClassTo && a.OverallStatus != "Cancelled").Select(a => a.GlAccount).FirstOrDefault();
                            string cashAccountCr = db.InstitutionAccounts.Where(a => a.AccountNumber == bank_account_from & a.InstitutionCode == contract.InstitutionCode && a.SubBudgetClass == contract.SubBudgetClass && a.OverallStatus != "Cancelled").Select(a => a.GlAccount).FirstOrDefault();
                            if (cashAccountDr == null)
                            {
                                processResponse.OverallStatus = "Error";
                                processResponse.OverallStatusDescription = "Transfer Retention,Debit Account Setup for " + receivingSummary.InstitutionCodeTo + " Does not Exist, Contact System Administrator ";
                                return processResponse;
                            }
                            else if (cashAccountDr == null)
                            {
                                processResponse.OverallStatus = "Error";
                                processResponse.OverallStatusDescription = "Transfer Retention,Credit Account Setup for " + contract.InstitutionCode + " Does not Exist, Contact System Administrator ";
                                return processResponse;
                            }
                            else
                            {
                                foreach (ReceivingContractDetailView receivingContractDetailView in receivingContractDetailViewList)
                                {
                                    VoucherDetail voucherDetail = new VoucherDetail
                                    {
                                        PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                                        JournalTypeCode = "PV",
                                        DrGlAccount = receivingContractDetailView.GlAccount,
                                        DrGlAccountDesc = receivingContractDetailView.GlAccountDesc,
                                        CrGlAccount = crCodes.CrCoa,
                                        CrGlAccountDesc = crCodes.CrCoaDesc,
                                        FundingReferenceNo = receivingContractDetailView.FundingReference,
                                        OperationalAmount = receivingContractDetailView.Amount,
                                        BaseAmount = receivingContractDetailView.BaseAmount,
                                        //TaxId
                                        //TaxCode
                                        //TaxName
                                        //TaxRate
                                    };

                                    voucherDetailList.Add(voucherDetail);
                                }
                            }

                        }
                        else
                        {
                            foreach (ReceivingContractDetailView receivingContractDetailView in receivingContractDetailViewList)
                            {
                                VoucherDetail voucherDetail = new VoucherDetail
                                {
                                    PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                                    JournalTypeCode = "PV",
                                    DrGlAccount = receivingContractDetailView.GlAccount,
                                    DrGlAccountDesc = receivingContractDetailView.GlAccountDesc,
                                    CrGlAccount = crCodes.CrCoa,
                                    CrGlAccountDesc = crCodes.CrCoaDesc,
                                    FundingReferenceNo = receivingContractDetailView.FundingReference,
                                    OperationalAmount = receivingContractDetailView.Amount,
                                    BaseAmount = receivingContractDetailView.BaseAmount,
                                    //TaxId
                                    //TaxCode
                                    //TaxName
                                    //TaxRate
                                };

                                voucherDetailList.Add(voucherDetail);
                            }
                        }

                        db.VoucherDetails.AddRange(voucherDetailList);

                        db.SaveChanges();

                        paymentVoucher.PVNo = GetLegalNumber(db, receivingSummary.InstitutionCode, "V", paymentVoucher.PaymentVoucherId);
                        processResponse.StringReturnValue = paymentVoucher.PVNo;
                        db.SaveChanges();
                        processResponse.ReturnId = paymentVoucher.PaymentVoucherId;
                        processResponse.OverallStatus = "Success";
                    }
                    else
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Receiving Details not found";
                        return processResponse;
                    }

                    if (receivingSummary.HasLiquidatedDamage)//1
                    {
                        PaymentVoucherDeductionType dType =
                            db.PaymentVoucherDeductionTypes
                            .Where(a => a.DeductionTypeName == "Liquidated Damage" || a.DeductionTypeName == "LiquidatedDamage")
                            .FirstOrDefault();

                        if (dType != null)
                        {
                            JournalTypeView journalTypeView =
                                db.JournalTypeViews
                                .Where(a => a.JournalTypeCode == "GJ"
                                  && a.InstitutionCode == paymentVoucher.InstitutionCode
                                  && a.DrGfsCode == dType.DeductionGfsCode
                                  && a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                                .FirstOrDefault();

                            PaymentVoucherDeduction pvDeduction = new PaymentVoucherDeduction
                            {
                                PVNo = paymentVoucher.PVNo,
                                PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                                PaymentDeductionTypeId = dType.PaymentVoucherDeductionTypeId,
                                COA = journalTypeView == null ? "" : journalTypeView.DrCoa,
                                OperationalAmount = paymentVoucher.LiquidatedDemageAmount,
                                BaseAmount = paymentVoucher.LiquidatedDemageAmount,
                                InstitutionCode = paymentVoucher.InstitutionCode,
                                CreatedAt = DateTime.Now,
                                Status = "Active"
                            };

                            db.PaymentVoucherDeductions.Add(pvDeduction);
                            db.SaveChanges();
                        }
                    }

                    if (receivingSummary.HasRetention)//1
                    {
                        PaymentVoucherDeductionType dType =
                            db.PaymentVoucherDeductionTypes
                            .Where(a => a.DeductionTypeName == "Retention")
                            .FirstOrDefault();
                        if (dType != null)
                        {
                            JournalTypeView journalTypeView =
                               db.JournalTypeViews
                               .Where(a => a.JournalTypeCode == "GJ"
                                 && a.InstitutionCode == paymentVoucher.InstitutionCode
                                 && a.DrGfsCode == dType.DeductionGfsCode
                                 && a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                               .FirstOrDefault();

                            PaymentVoucherDeduction pvDeduction = new PaymentVoucherDeduction
                            {
                                PVNo = paymentVoucher.PVNo,
                                PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                                PaymentDeductionTypeId = dType.PaymentVoucherDeductionTypeId,
                                COA = journalTypeView == null ? "" : journalTypeView.DrCoa,
                                OperationalAmount = paymentVoucher.RetentionAmount,
                                BaseAmount = paymentVoucher.RetentionAmount,
                                InstitutionCode = paymentVoucher.InstitutionCode,
                                CreatedAt = DateTime.Now,
                                Status = "Active"
                            };

                            db.PaymentVoucherDeductions.Add(pvDeduction);
                            db.SaveChanges();
                        }
                    }

                    if (receivingSummary.AdvancePayment != null)//1
                    {
                        PaymentVoucherDeductionType dType =
                            db.PaymentVoucherDeductionTypes
                            .Where(a => a.DeductionTypeName == "Advance Payment" || a.DeductionTypeName == "AdvancePayment")
                            .FirstOrDefault();
                        if (dType != null)
                        {
                            JournalTypeView journalTypeView =
                               db.JournalTypeViews
                               .Where(a => a.JournalTypeCode == "GJ"
                                 && a.InstitutionCode == paymentVoucher.InstitutionCode
                                 && a.DrGfsCode == dType.DeductionGfsCode
                                 && a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                               .FirstOrDefault();

                            PaymentVoucherDeduction pvDeduction = new PaymentVoucherDeduction
                            {
                                PVNo = paymentVoucher.PVNo,
                                PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                                PaymentDeductionTypeId = dType.PaymentVoucherDeductionTypeId,
                                COA = journalTypeView == null ? "" : journalTypeView.DrCoa,
                                OperationalAmount = paymentVoucher.AdvancePayment,
                                BaseAmount = paymentVoucher.AdvancePayment,
                                InstitutionCode = paymentVoucher.InstitutionCode,
                                CreatedAt = DateTime.Now,
                                Status = "Active"
                            };

                            db.PaymentVoucherDeductions.Add(pvDeduction);
                            db.SaveChanges();
                        }
                    }
                }
                // Bulk Payment
                else if (journalCode == "BPV")
                {

                    PaymentBatch paymentBatch = db.PaymentBatches.Find(id);
                    if (paymentBatch == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Payment Batch Transaction not found";
                        return processResponse;
                    }

                    var payerBank = db.InstitutionAccounts.Where(a => a.SubBudgetClass == paymentBatch.SubBudgetClass && a.InstitutionCode == paymentBatch.InstitutionCode && a.OverallStatus != "Cancelled").FirstOrDefault();
                    if (payerBank == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Account Setup is Incomplete. There is no expenditure account for sub budget class '" + paymentBatch.SubBudgetClass + "'. Please consult Administrator!";
                        return processResponse;
                    }
                    var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode.ToUpper() == paymentBatch.PayeeType.ToUpper() && a.Status != "Cancelled").FirstOrDefault();

                    if (payeeType == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Vendor setup is incomplete. There is no payee type setup for '" + paymentBatch.PayeeType + "'. Please contact Administrator!";
                        return processResponse;
                    }
                    var crCodes = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.GfsCode && a.SubBudgetClass == paymentBatch.SubBudgetClass && a.InstitutionCode == paymentBatch.InstitutionCode).FirstOrDefault();
                    if (crCodes == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Chart of Account setup is incomplete. COA with GFS '" + payeeType.GfsCode + "' for subbudget class '" + paymentBatch.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return processResponse;
                    }

                    var unappliedAccount = db.InstitutionAccounts
                   .Where(a => a.InstitutionCode == paymentBatch.InstitutionCode
                     && a.AccountType.ToUpper() == "UNAPPLIED"
                     && a.OverallStatus != "Cancelled"
                     && a.IsTSA == false
                   ).FirstOrDefault();

                    if (unappliedAccount == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Institution Bank Account Setup is Incomplete. There is no unapplied account for the institution'" + paymentBatch.InstitutionName + "'. Please consult Administrator!";
                        return processResponse;
                    }
                    paymentVoucher = new PaymentVoucher
                    {
                        //  PVNo,
                        SourceModule = "BulkPayment",
                        SourceModuleReferenceNo = paymentBatch.BatchNo,
                        JournalTypeCode = "PV",
                        Narration = paymentBatch.BatchDesc,
                        PaymentDesc = paymentBatch.BatchDesc,
                        PayeeDetailId = paymentBatch.PayeeDetailId,
                        PayeeCode = paymentBatch.PayeeCode,
                        Payeename = paymentBatch.Payeename,
                        PayeeBankAccount = paymentBatch.PayeeBankAccount,
                        PayeeBankName = paymentBatch.PayeeBankName,
                        PayeeAccountName = paymentBatch.PayeeBankAccount,
                        //PayeeAddress = paymentBatch.Payeeaddress,
                        PayeeBIC = paymentBatch.PayeeBIC,
                        PayeeType = paymentBatch.PayeeType,
                        PayerBankAccount = payerBank.AccountNumber,
                        PayerBankName = payerBank.AccountName,
                        PayerBIC = payerBank.BIC,
                        PayerCashAccount = payerBank.GlAccount,
                        PayerAccountType = payerBank.AccountType,
                        OperationalAmount = paymentBatch.TotalAmount,
                        BaseAmount = paymentBatch.BaseAmount,
                        BaseCurrency = paymentBatch.BaseCurrency,
                        OperationalCurrency = paymentBatch.OperationalCurrency,
                        ExchangeRate = paymentBatch.ExchangeRate,
                        ApplyDate = DateTime.Now,
                        SubBudgetClass = paymentBatch.SubBudgetClass,
                        PaymentMethod = "EFT",
                        FinancialYear = GetFinancialYear(db, DateTime.Now),
                        CreatedBy = loggedInUser.Identity.Name,
                        CreatedAt = DateTime.Now,
                        OverallStatus = "Pending",
                        Book = "Main",
                        InstitutionId = paymentBatch.InstitutionId,
                        InstitutionCode = paymentBatch.InstitutionCode,
                        InstitutionName = paymentBatch.InstitutionName,
                        PaystationId = paymentBatch.PaystationId,
                        SubLevelCategory = paymentBatch.SubLevelCategory,
                        SubLevelCode = paymentBatch.SubLevelCode,
                        SubLevelDesc = paymentBatch.SubLevelDesc,
                        ReversalFlag = false,
                        GeneralLedgerStatus = "Pending",
                        QueueId = 0,
                        OverallStatusDesc = "Pending",
                        PayableGlAccount = crCodes.CrCoa,
                        UnappliedAccount = unappliedAccount.AccountNumber,
                        InstitutionAccountId = payerBank.InstitutionAccountId,
                        OtherSourceId = id,

                    };

                    if (paymentVoucher == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Error saving payment voucher";
                        return processResponse;
                    }
                    db.PaymentVouchers.Add(paymentVoucher);


                    List<VoucherDetail> voucherDetailList = new List<VoucherDetail>();

                    List<PaymentBatchCoa> paymentBatchCoaList = db.PaymentBatchCoas.Where(a => a.PaymentBatchId == paymentBatch.PaymentBatchID).ToList();


                    foreach (PaymentBatchCoa paymentBatchCoaDetail in paymentBatchCoaList)
                    {
                        VoucherDetail voucherDetail = new VoucherDetail
                        {
                            PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                            JournalTypeCode = "PV",
                            DrGlAccount = paymentBatchCoaDetail.DrGlAccount,
                            DrGlAccountDesc = paymentBatchCoaDetail.DrGlAccountDesc,
                            CrGlAccount = crCodes.CrCoa,
                            CrGlAccountDesc = crCodes.CrCoaDesc,
                            FundingReferenceNo = paymentBatchCoaDetail.FundingReferenceNo,
                            OperationalAmount = paymentBatchCoaDetail.OperationalAmount,
                            BaseAmount = paymentBatchCoaDetail.BaseAmount,

                        };

                        voucherDetailList.Add(voucherDetail);
                    }
                    db.VoucherDetails.AddRange(voucherDetailList);
                    db.SaveChanges();

                    paymentVoucher.PVNo = GetLegalNumber(db, paymentBatch.InstitutionCode, "V", paymentVoucher.PaymentVoucherId);
                    paymentBatch.PVNo = paymentVoucher.PVNo;
                    paymentBatch.PaymentVoucherId = paymentVoucher.PaymentVoucherId;
                    paymentBatch.PrefundingRef = paymentVoucher.PVNo;

                    db.SaveChanges();

                    processResponse.ReturnId = paymentVoucher.PaymentVoucherId;
                    processResponse.OverallStatus = "Success";
                }

                //End Bulk Payment


            }
            catch (Exception ex)
            {
                processResponse.OverallStatus = "Error";
                processResponse.OverallStatusDescription = ex.Message.ToString();//processResponse.OverallStatusDescription;
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return processResponse;
        }
        public static ProcessResponse CreatePaymentEFT(List<PaymentVoucher> paymentVoucherList, PaymentFile paymentFile, IFMISTZDbContext db)
        {
            ProcessResponse eftStatus = new ProcessResponse();
            eftStatus.OverallStatus = "Pending";
            var receiverBic = "";

            try
            {

                SystemConfig eftConfig = db.SystemConfigs.Where(a => a.ConfigName == "EFTFilePath").FirstOrDefault();
                if (eftConfig == null)
                {
                    eftStatus.OverallStatus = "Error";
                    eftStatus.OverallStatusDescription = "EFT File Path configuration is incomplete. Please contact system administrator!";
                    return eftStatus;
                }
                string str_base_path = eftConfig.ConfigValue;

                SystemConfig xmlConfig = db.SystemConfigs.Where(a => a.ConfigName == "XMLSchemaPath").FirstOrDefault();
                if (xmlConfig == null)
                {
                    eftStatus.OverallStatus = "Error";
                    eftStatus.OverallStatusDescription = "XML schema configuration is incomplete. Please contact system administrator!";
                    return eftStatus;
                }
                string strXMLSchemaPath = xmlConfig.ConfigValue;

                string xml_line = "", filePath, strSignedFilePath, strXMLErrorFilePath;

                bool exists = System.IO.Directory.Exists(str_base_path);



                if (!exists)
                    System.IO.Directory.CreateDirectory(str_base_path);
                //Get payment totals
                int NbOfTxs = paymentVoucherList.Count();
                // decimal TotalAmount = (decimal)paymentVoucherList.Sum(a => a.NetOperationalAmount);//...

                decimal TotalAmount = 0;

                foreach (var paymentVoucher in paymentVoucherList)
                {
                    decimal OperationalAmount = (decimal)paymentVoucher.OperationalAmount;
                    if (paymentVoucher.hasWithHolding)
                    {
                        OperationalAmount = OperationalAmount - (decimal)paymentVoucher.OperationalWithHoldingAmount;
                    }

                    if (paymentVoucher.LiquidatedDemageAmount != null)
                    {
                        OperationalAmount = (decimal)(OperationalAmount - paymentVoucher.LiquidatedDemageAmount);
                    }

                    if (paymentVoucher.RetentionAmount != null)
                    {
                        OperationalAmount = (decimal)(OperationalAmount - paymentVoucher.RetentionAmount);
                    }

                    if (paymentVoucher.AdvancePayment != null)
                    {
                        OperationalAmount = (decimal)(OperationalAmount - paymentVoucher.AdvancePayment);
                    }
                    // CHECKS END
                    TotalAmount += OperationalAmount;
                }
                //////////                
                int FinancialYear = GetFinancialYear(db, DateTime.Now);
                int fileNumber = paymentFile.PaymentFileId;

                string currentMsgId = "MUSP" + FinancialYear.ToString().Substring(2, 2) + paymentFile.NumRejections.ToString().PadLeft(2, '0') + fileNumber.ToString().PadLeft(7, '0');
                paymentFile.MsgId = currentMsgId;
                eftStatus.StrReturnId = currentMsgId;

                filePath = str_base_path + "\\" + paymentFile.MsgId + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + ".xml";
                strSignedFilePath = str_base_path + "\\" + paymentFile.MsgId + DateTime.Now.ToString("yyyyMMddHHmmss") + "-SIGNED.xml";
                strXMLErrorFilePath = str_base_path + "\\" + paymentFile.MsgId + DateTime.Now.ToString("yyyyMMddHHmmss") + "-INVALID.xml";

                //Add dynamic values to eft file
                receiverBic = db.Accounts.Where(a => a.AccountNo == paymentFile.PayerBankAccount).Select(a => a.BankBIC).FirstOrDefault();

                var xml_text = new StringBuilder();
                xml_line = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>";
                xml_line += "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_block_payment.xsd\" >";
                xml_line += "<Header>";

                xml_line += "<Sender>MOFPTZTZ</Sender>";
                xml_line += "<Receiver>" + receiverBic + "</Receiver>";

                xml_line += "<MsgId>" + paymentFile.MsgId + "</MsgId>";
                xml_line += "<PaymentType>" + "P108" + "</PaymentType>";
                xml_line += "<MessageType>Payment</MessageType>";

                xml_line += "</Header>";

                /*** Begin Payment Block ****/
                xml_line += "<BlockPayment>";


                xml_line += "<MsgSummary>";

                xml_line += "<TransferRef>" + paymentFile.FundingRef + "</TransferRef>";


                xml_line += "<CreDtTm>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</CreDtTm>";
                xml_line += "<NbOfTxs>" + NbOfTxs.ToString() + "</NbOfTxs>";
                xml_line += "<Currency>TZS</Currency>";
                xml_line += "<TotalAmount>" + TotalAmount.ToString("0.00") + "</TotalAmount>";
                xml_line += "<PayerName>" + paymentFile.PayerBankName.Replace("+", "").Replace("'", "").Replace("-", "").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "") + "</PayerName>";
                xml_line += "<PayerAcct>" + paymentFile.PayerBankAccount.Replace(" ", "").Replace("\t", "").Replace("+", "").Replace("'", "").Replace("-", "").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "") + "</PayerAcct>";
                xml_line += "<RegionCode>" + paymentFile.BotRegionCode + "</RegionCode>";


                xml_line += "</MsgSummary>";


                xml_text.Append(xml_line);
                string priority = "0";
                string disbNum = "";

                foreach (var paymentVoucher in paymentVoucherList)
                {
                    disbNum = "";
                    if (paymentVoucher.Priority == "High")
                        priority = "1";
                    else
                        priority = "0";


                    if (paymentVoucher.ControlNumber != null && paymentVoucher.ControlNumber != "")
                    {
                        if (paymentVoucher.ControlNumber.Replace(" ", "").Length >= 6)
                        {
                            disbNum = paymentVoucher.ControlNumber.Replace(" ", "");
                            priority = "1";
                        }

                    }

                    xml_line = "<TrxRecord>";

                    xml_line += "<Priority>" + priority + "</Priority>";
                    if (paymentVoucher.PayeeCode != null && paymentVoucher.PayeeCode != "")
                        xml_line += "<VendorNo>" + paymentVoucher.PayeeCode.Replace(" ", "").Replace("-", "") + "</VendorNo>";
                    string paymentDescription = paymentVoucher.PaymentDesc.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "");
                    if (paymentDescription.Length >= 80)
                    {
                        paymentDescription = paymentDescription.Substring(0, 79);
                    }

                    // OTHER CHECKS
                    decimal OperationalAmount = (decimal)paymentVoucher.OperationalAmount;
                    if (paymentVoucher.hasWithHolding)
                    {
                        OperationalAmount = OperationalAmount - (decimal)paymentVoucher.OperationalWithHoldingAmount;
                    }

                    if (paymentVoucher.LiquidatedDemageAmount != null)
                    {
                        OperationalAmount = (decimal)(OperationalAmount - paymentVoucher.LiquidatedDemageAmount);
                    }

                    if (paymentVoucher.RetentionAmount != null)
                    {
                        OperationalAmount = (decimal)(OperationalAmount - paymentVoucher.RetentionAmount);
                    }
                    if (paymentVoucher.AdvancePayment != null)
                    {
                        OperationalAmount = (decimal)(OperationalAmount - paymentVoucher.AdvancePayment);
                    }
                    // CHECKS END


                    xml_line += "<EndToEndId>" + paymentVoucher.PVNo + "</EndToEndId>";
                    // xml_line += "<TrxAmount>" + ((decimal)paymentVoucher.NetOperationalAmount).ToString("0.00") + "</TrxAmount>";
                    xml_line += "<TrxAmount>" + OperationalAmount.ToString("0.00") + "</TrxAmount>";
                    xml_line += "<BenName>" + paymentVoucher.PayeeAccountName.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenName>";
                    xml_line += "<BenAcct>" + paymentVoucher.PayeeBankAccount.Replace("/", "").Replace("-", "").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace(" ", "").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenAcct>";
                    xml_line += "<BenBic>" + paymentVoucher.PayeeBIC.Replace("/", "").Replace("-", "").Replace(".", "").Replace(" ", "").Replace(",", "") + "</BenBic>";
                    xml_line += "<Description>" + paymentDescription + "</Description>";

                    if (disbNum.Length < 6)
                    {
                        disbNum = paymentVoucher.PaymentVoucherId.ToString().PadLeft(6, '0');
                    }
                    xml_line += "<DisbNum>" + disbNum + "</DisbNum>";

                    //   xml_line += "<OCustomer>" + paymentVoucher.InstitutionName.Replace(Environment.NewLine, "").ToLower().Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</OCustomer>";

                    xml_line += "<UnappliedAccount>" + paymentVoucher.UnappliedAccount + "</UnappliedAccount>";
                    xml_line += "<DetailsOfCharges>" + "SHA" + "</DetailsOfCharges>";

                    xml_line += "</TrxRecord>";
                    xml_text.Append(xml_line);

                }
                xml_line = "</BlockPayment>";
                /**** End of Payment Block ****/
                xml_text.Append(xml_line);
                xml_line = "</Document>";
                xml_text.Append(xml_line);
                File.WriteAllText(filePath, xml_text.ToString());//UNSIGNED XML FILE
                XMLValidator xmlValidationResponse = XMLTools.validateXMLFile(filePath, "Payment", strXMLSchemaPath);
                if (xmlValidationResponse.blnFileIsValid)
                {
                    string signedHashString = DigitalSignature.GenerateDigitalSignature(filePath, true, "EFT");
                    if (signedHashString.Contains("Error"))
                    {
                        eftStatus.OverallStatus = signedHashString;
                        eftStatus.OverallStatusDescription = signedHashString;
                        return eftStatus;
                    }
                    File.WriteAllText(strSignedFilePath, xml_text.ToString() + "|" + signedHashString); //SIGNED XML FILE
                    eftStatus.StringReturnValue = strSignedFilePath;

                    SystemConfig receiverUrlConfig;
                    switch (receiverBic)
                    {
                        case "TANZTZTX":
                            receiverUrlConfig = db.SystemConfigs.Where(a => a.ConfigName == "BOTUrl").FirstOrDefault();
                            break;
                        case "NMIBTZTZ":
                            receiverUrlConfig = db.SystemConfigs.Where(a => a.ConfigName == "NMBUrl").FirstOrDefault();
                            break;
                        case "CORUTZTZ":
                            receiverUrlConfig = db.SystemConfigs.Where(a => a.ConfigName == "CRDBUrl").FirstOrDefault();
                            break;
                        default:
                            receiverUrlConfig = new SystemConfig();
                            break;
                    }


                    if (receiverUrlConfig == null)
                    {
                        eftStatus.OverallStatus = "Error";
                        eftStatus.OverallStatusDescription = "XML schema configuration is incomplete. Please contact system administrator!";
                        return eftStatus;
                    }
                    string receiverUrl = receiverUrlConfig.ConfigValue;
                    string sendStatus = "OK";
                    if (Properties.Settings.Default.HostingEnvironment == "Live" && receiverBic == "TANZTZTX")
                    {
                        sendStatus = SendToBOT(strSignedFilePath, "EFT", receiverUrl);
                    }

                    if (sendStatus == "OK")
                    {
                        eftStatus.OverallStatus = "Success";
                    }
                    else
                    {

                        eftStatus.OverallStatus = "Error";
                        eftStatus.OverallStatusDescription = "Error Sending file(s) to BOT. Please contact Administrator";
                        return eftStatus;
                    }
                }
                else //Invalid XML file. 
                {
                    File.Move(filePath, strXMLErrorFilePath);
                    eftStatus.OverallStatus = "Invalid Tiss File";
                    eftStatus.OverallStatusDescription = string.Join(" *,* ", xmlValidationResponse.Errors);
                    System.IO.File.WriteAllText(strXMLErrorFilePath + "_Errorlog", eftStatus.OverallStatusDescription);//Log the XML errors for further action
                }

                return eftStatus;
            }
            catch (SqlException exSql)
            {
                eftStatus.OverallStatus = "DB Exception";
                eftStatus.OverallStatusDescription = "SQL Error:" + exSql.ToString();
                return eftStatus;
            }

        }

        public static string SendToBOT(string strFilePath, string strMessageType, string strBOTUrl)
        {
            string response = "No Response";
            try
            {
                if (strMessageType == "EFT")
                {
                    //Creating Request of the destination URL
                    HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(strBOTUrl);

                    httpRequest.Method = "POST";
                    httpRequest.ReadWriteTimeout = 16000000;
                    httpRequest.Timeout = 16000000;

                    //Defining the type of the posted data as XML
                    httpRequest.ContentType = "text/xml";

                    string data = File.ReadAllText(strFilePath);//"Hello World!"; //
                    /**  LIMITS OF THE SIZE OF STRING VARIABLE (file size)
                     * The theoretical limit may be 2,147,483,647, but the practical limit is nowhere near that. Since no single object in a .Net program may be over 2GB 
                     * and the string type uses unicode (2 bytes for each character), the best you could do is 1,073,741,823, 
                     * but you're not likely to ever be able to allocate that on a 32-bit machine.
                     * 

                    **/
                    byte[] bytedata = Encoding.UTF8.GetBytes(data);
                    // Get the request stream.
                    Stream requestStream = httpRequest.GetRequestStream();
                    requestStream.WriteTimeout = 160000000;
                    requestStream.ReadTimeout = 160000000;
                    // Write the data to the request stream.
                    requestStream.Write(bytedata, 0, bytedata.Length);
                    requestStream.Close();
                    //Get Response
                    HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    string strResponse = "Status Code: " + httpResponse.StatusCode.ToString() + Environment.NewLine
                        + "Status Description: " + httpResponse.StatusDescription.ToString() + Environment.NewLine
                        + "ResponseUri:" + httpResponse.ResponseUri.ToString() + Environment.NewLine
                        + "Server:" + httpResponse.Server.ToString() + Environment.NewLine + Environment.NewLine
                        + "Headers:" + httpResponse.Headers.ToString();

                    File.WriteAllText(strFilePath + "_Response", strResponse);//Log the http response from BOT.
                    response = httpResponse.StatusCode.ToString();
                }
                else if (strMessageType == "TISS")
                {
                    response = "TISS";
                }
            }
            catch (Exception ex)
            {
                response = ex.Message.ToString();
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return response;
        }

        public static HttpWebResponse SendToCommercialBank(string data, string strUrl)
        {
            HttpWebResponse httpWebResponse = null;
            try
            {
                //Creating Request of the destination URL
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(strUrl);

                httpWebRequest.Method = "POST";
                httpWebRequest.ReadWriteTimeout = 16000000;
                httpWebRequest.Timeout = 16000000;

                //Defining the type of the posted data as XML
                httpWebRequest.ContentType = "text/xml";

                /**  LIMITS OF THE SIZE OF STRING VARIABLE (file size)
                 * The theoretical limit may be 2,147,483,647, but the practical limit is nowhere near that. Since no single object in a .Net program may be over 2GB 
                 * and the string type uses unicode (2 bytes for each character), the best you could do is 1,073,741,823, 
                 * but you're not likely to ever be able to allocate that on a 32-bit machine.
                 * 

                **/
                byte[] bytedata = Encoding.UTF8.GetBytes(data);
                // Get the request stream.
                Stream requestStream = httpWebRequest.GetRequestStream();
                requestStream.WriteTimeout = 160000000;
                requestStream.ReadTimeout = 160000000;
                // Write the data to the request stream.
                requestStream.Write(bytedata, 0, bytedata.Length);
                requestStream.Close();
                //Get Response
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (ProtocolViolationException ex)
            {
                Log.Information(ex.Message + "{Name}!", "SendToBankExceptions");
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (WebException ex)
            {
                Log.Information(ex.Message + "{Name}!", "SendToBankExceptions");
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (InvalidOperationException ex)
            {
                Log.Information(ex.Message + "{Name}!", "SendToBankExceptions");
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (NotSupportedException ex)
            {
                Log.Information(ex.Message + "{Name}!", "SendToBankExceptions");
                ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message + "{Name}!", "SendToBankExceptions");
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return httpWebResponse;
        }

        public static string GetSystemConfig(string strConfigName)
        {
            IFMISTZDbContext db = new IFMISTZDbContext();
            string strConfig = "";
            var configs = db.SystemConfigs.Where(a => a.ConfigName == strConfigName).ToList();

            if (configs.Count() > 0)
                strConfig = configs[0].ConfigValue.ToString();

            return strConfig;
        }


        internal static List<PaymentBatch> GetPaymentBatchList(IFMISTZDbContext db)
        {
            var paymentBatchList = (from a in db.PaymentBatches
                                    where a.OverallStatus.Trim() == "Pending"
                                    select a).Distinct().ToList();
            return paymentBatchList;
        }

        internal static List<string> GetStatue()
        {
            var list = new List<string>();
            list.Add("All");
            list.Add("Pending");
            list.Add("Confirmed");
            list.Add("Approved");
            list.Add("Posted");

            return list;
        }
        internal static List<string> GetPayeeStatue()
        {
            var list = new List<string>();
            list.Add("All");
            list.Add("Pending");
            list.Add("Confirmed");
            list.Add("Approved");
            list.Add("Active");
            list.Add("Inactive");

            return list;
        }
        public static ProcessResponse GenerateWithHolding(IFMISTZDbContext db, int paymentVoucherId)
        {
            ProcessResponse processResponse = new ProcessResponse();
            processResponse.OverallStatus = "Pending";
            processResponse.ReturnId = 0;

            try
            {
                PaymentVoucher paymentVoucher = db.PaymentVouchers.Find(paymentVoucherId);
                if (paymentVoucher == null)
                {
                    processResponse.OverallStatus = "Error";
                    processResponse.OverallStatusDescription = "Withholding Generation Failed. Payment Voucher Transaction not found";
                    return processResponse;
                }

                if (paymentVoucher.hasWithHolding)
                {
                    var payeeType = db.PayeeTypes.Where(a => a.PayeeTypeCode == paymentVoucher.PayeeType && a.Status != "Cancelled").FirstOrDefault();

                    var withHoldingCoa = db.JournalTypeViews.Where(a => a.CrGfsCode == payeeType.WithheldGfsCode && a.SubBudgetClass == paymentVoucher.SubBudgetClass && a.InstitutionCode == paymentVoucher.InstitutionCode).FirstOrDefault();
                    if (withHoldingCoa == null)
                    {
                        processResponse.OverallStatus = "Error";
                        processResponse.OverallStatusDescription = "Withholding Generation Failed. Chart of Account setup is incomplete. Withholding COA with GFS '" + payeeType.WithheldGfsCode + "' for subbudget class '" + paymentVoucher.SubBudgetClass + "' is missing. Please contact Administrator!";
                        return processResponse;
                    }

                    decimal withHoldingAmount = (decimal)paymentVoucher.GoodsAmount * (decimal)0.02 + (decimal)paymentVoucher.ServiceAmount * (decimal)0.05;
                    paymentVoucher.NetOperationalAmount = Math.Round((decimal)(paymentVoucher.OperationalAmount - withHoldingAmount), 2);
                    paymentVoucher.NetBaseAmount = Math.Round((decimal)(paymentVoucher.OperationalAmount - withHoldingAmount), 2);
                    paymentVoucher.WithHoldingCoaCode = withHoldingCoa.CrCoa;
                    paymentVoucher.WithHoldingCoaDesc = withHoldingCoa.CrCoaDesc;

                    WithHoldingDetail withHoldingDetail = new WithHoldingDetail
                    {
                        PVNo = paymentVoucher.PVNo,
                        PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                        PayeeDetailId = paymentVoucher.PayeeDetailId,
                        PayeeCode = paymentVoucher.PayeeCode,
                        Payeename = paymentVoucher.Payeename,
                        PayeeType = paymentVoucher.PayeeType,
                        Narration = paymentVoucher.Narration,
                        ServiceAmount = paymentVoucher.ServiceAmount != null ? (decimal)paymentVoucher.ServiceAmount : 0,
                        VATOnService = paymentVoucher.VATOnService != null?(decimal)paymentVoucher.VATOnService:0,
                        GoodsAmount = paymentVoucher.GoodsAmount != null? (decimal)paymentVoucher.GoodsAmount:0,
                        VATOnGoods = paymentVoucher.VATOnGoods !=null?(decimal)paymentVoucher.VATOnGoods:0,
                        OperationalWithHoldingAmount = (decimal)paymentVoucher.OperationalWithHoldingAmount,
                        BaseWithHoldingAmount = (decimal)paymentVoucher.BaseWithHoldingAmount,
                        WithHoldingCoaCode = paymentVoucher.WithHoldingCoaCode,
                        WithHoldingCoaDesc = paymentVoucher.WithHoldingCoaDesc,
                        VoucherOperationalAmount = (decimal)paymentVoucher.OperationalAmount,
                        VoucherBaseAmount = (decimal)paymentVoucher.BaseAmount,
                        BaseCurrencyId = paymentVoucher.BaseCurrencyId,
                        BaseCurrency = paymentVoucher.BaseCurrency,
                        OperationalCurrencyId = paymentVoucher.OperationalCurrencyId,
                        OperationalCurrency = paymentVoucher.OperationalCurrency,
                        ExchangeRate = paymentVoucher.ExchangeRate,
                        ApplyDate = paymentVoucher.ApplyDate,
                        FinancialPeriodId = paymentVoucher.FinancialPeriodId,
                        FinancialYear = paymentVoucher.FinancialYear,
                        CreatedBy = paymentVoucher.CreatedBy,
                        CreatedAt = paymentVoucher.CreatedAt,
                        Book = paymentVoucher.Book,
                        InstitutionId = paymentVoucher.InstitutionId,
                        InstitutionCode = paymentVoucher.InstitutionCode,
                        InstitutionName = paymentVoucher.InstitutionName,
                        PaystationId = paymentVoucher.PaystationId,
                        SubLevelCategory = paymentVoucher.SubLevelCategory,
                        SubLevelCode = paymentVoucher.SubLevelCode,
                        SubLevelDesc = paymentVoucher.SubLevelDesc,
                        SubBudgetClass = paymentVoucher.SubBudgetClass,
                        OverallStatus = "Pending",
                        OverallStatusDesc = "Pending",
                        JournalTypeCode = paymentVoucher.JournalTypeCode,
                        ReversalFlag = false,
                        GeneralLedgerStatus = "Pending",

                    };
                    db.WithHoldingDetails.Add(withHoldingDetail);

                    PaymentVoucherDeductionType dType =
                        db.PaymentVoucherDeductionTypes
                      .Where(a => a.DeductionTypeName == "WithHoldingTax")
                      .FirstOrDefault();

                    if (dType != null)
                    {
                        JournalTypeView journalTypeView =
                              db.JournalTypeViews
                              .Where(a => a.JournalTypeCode == "GJ"
                                && a.InstitutionCode == paymentVoucher.InstitutionCode
                                && a.DrGfsCode == dType.DeductionGfsCode
                                && a.SubBudgetClass == paymentVoucher.SubBudgetClass)
                              .FirstOrDefault();

                        if (journalTypeView == null)
                        {
                            processResponse.OverallStatus = "Error";
                            processResponse.OverallStatusDescription =
                                "COA setup is Missing for SubbudgetClass=" + paymentVoucher.SubBudgetClass +
                                " DeduductionGFSCode =" + dType.DeductionGfsCode +
                                " InstititionCode =" + paymentVoucher.InstitutionCode +
                                " and JournalTypeCode='PV'";
                        }

                        PaymentVoucherDeduction pvDeduction = new PaymentVoucherDeduction
                        {
                            PVNo = paymentVoucher.PVNo,
                            PaymentVoucherId = paymentVoucher.PaymentVoucherId,
                            PaymentDeductionTypeId = dType.PaymentVoucherDeductionTypeId,
                            COA = journalTypeView == null ? "" : journalTypeView.DrCoa,
                            OperationalAmount = paymentVoucher.OperationalWithHoldingAmount,
                            BaseAmount = paymentVoucher.BaseWithHoldingAmount,
                            InstitutionCode = paymentVoucher.InstitutionCode,
                            CreatedAt = DateTime.Now,
                            Status = "Active"
                        };

                        db.PaymentVoucherDeductions.Add(pvDeduction);
                    }


                    db.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                processResponse.OverallStatus = "Error";
                processResponse.OverallStatusDescription = "Withholding Generation Failed. " + ex.Message.ToString();
            }
            return processResponse;
        }
        public static ProcessResponse CreateBulkPaymentEFT(List<BulkPayment> BulkyPaymentList, PaymentBatch PaymentBatchFile, IFMISTZDbContext db)
        {
            ProcessResponse eftStatus = new ProcessResponse();
            eftStatus.OverallStatus = "Pending";
            try
            {
                bool debugMode = false;

                SystemConfig eftConfig = db.SystemConfigs.Where(a => a.ConfigName == "EFTFilePath").FirstOrDefault();
                if (eftConfig == null)
                {
                    eftStatus.OverallStatus = "Error";
                    eftStatus.OverallStatusDescription = "EFT File Path configuration is incomplete. Please contact system administrator!";
                    return eftStatus;
                }
                string str_base_path = eftConfig.ConfigValue;

                SystemConfig xmlConfig = db.SystemConfigs.Where(a => a.ConfigName == "XMLSchemaPath").FirstOrDefault();
                if (xmlConfig == null)
                {
                    eftStatus.OverallStatus = "Error";
                    eftStatus.OverallStatusDescription = "XML schema configuration is incomplete. Please contact system administrator!";
                    return eftStatus;
                }
                string strXMLSchemaPath = xmlConfig.ConfigValue;

                string xml_line = "", filePath, strSignedFilePath, strXMLErrorFilePath;

                bool exists = System.IO.Directory.Exists(str_base_path);



                if (!exists)
                    System.IO.Directory.CreateDirectory(str_base_path);
                //Get payment totals
                int NbOfTxs = (int)PaymentBatchFile.NoTrx;
                decimal TotalAmount = (decimal)PaymentBatchFile.TotalAmount;
                //MessageId
                int financialyear = ServiceManager.GetFinancialYear(db, DateTime.Now);
                //string MsgId = "MUSB" + financialyear.ToString() + PaymentBatchFile.NumSubmissions.ToString().PadLeft(2, '0') + PaymentBatchFile.PaymentBatchID.ToString().PadLeft(8, '0');
                string MsgId = PaymentBatchFile.MsgID;

                filePath = str_base_path + MsgId + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xml";
                strSignedFilePath = str_base_path + MsgId + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-SIGNED.xml";
                strXMLErrorFilePath = str_base_path + MsgId + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-INVALID.xml";
                //filePath = str_base_path + "\\MUSB-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + MsgId + ".xml";
                //strSignedFilePath = str_base_path + "\\MUSB-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + MsgId + "-SIGNED.xml";
                //strXMLErrorFilePath = str_base_path + "\\MUSB-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + MsgId + "-INVALID.xml";
                var xml_text = new StringBuilder();
                xml_line = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>";
                xml_line += "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_block_payment.xsd\" >";
                xml_line += "<Header>";

                xml_line += "<Sender>MOFPTZTZ</Sender>";
                xml_line += "<Receiver>TANZTZTX</Receiver>";

                xml_line += "<MsgId>" + MsgId + "</MsgId>";
                xml_line += "<PaymentType>" + "P003" + "</PaymentType>";
                xml_line += "<MessageType>Payment</MessageType>";

                xml_line += "</Header>";

                /*** Begin Payment Block ****/
                xml_line += "<BlockPayment>";


                xml_line += "<MsgSummary>";

                xml_line += "<TransferRef>" + PaymentBatchFile.PrefundingRef + "</TransferRef>";
                xml_line += "<CreDtTm>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</CreDtTm>";
                xml_line += "<NbOfTxs>" + NbOfTxs.ToString() + "</NbOfTxs>";
                xml_line += "<Currency>TZS</Currency>";
                xml_line += "<TotalAmount>" + TotalAmount.ToString("0.00") + "</TotalAmount>";
                xml_line += "<PayerName>" + PaymentBatchFile.PayerBankName.Replace("+", "").Replace("'", "").Replace("-", "").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "") + "</PayerName>";
                xml_line += "<PayerAcct>" + PaymentBatchFile.PayerBankAccount + "</PayerAcct>";
                xml_line += "<RegionCode>" + "TZDO" + "</RegionCode>";


                xml_line += "</MsgSummary>";


                xml_text.Append(xml_line);
                string priority = "0";
                string endToEndId = "";
                string disbNum = "";
                foreach (var bulkpayment in BulkyPaymentList)
                {
                    endToEndId = PaymentBatchFile.InstitutionCode + "E" + bulkpayment.BulkPaymentID.ToString().PadLeft(6, '0');
                    disbNum = bulkpayment.BulkPaymentID.ToString().PadLeft(6, '0');
                    xml_line = "<TrxRecord>";
                    xml_line += "<Priority>" + priority + "</Priority>";
                    if (bulkpayment.BeneficiaryCode != null && bulkpayment.BeneficiaryCode != "")
                        xml_line += "<VendorNo>" + bulkpayment.BeneficiaryCode.Replace(" ", "").Replace("-", "") + "</VendorNo>";
                    xml_line += "<EndToEndId>" + endToEndId + "</EndToEndId>";
                    xml_line += "<TrxAmount>" + ((decimal)bulkpayment.Amount).ToString("0.00") + "</TrxAmount>";
                    xml_line += "<BenName>" + bulkpayment.BeneficiaryName.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenName>";
                    xml_line += "<BenAcct>" + bulkpayment.BeneficiaryAccountNo.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenAcct>";
                    xml_line += "<BenBic>" + bulkpayment.BankBic.Replace("/", "").Replace("-", "").Replace(".", "").Replace(" ", "").Replace(",", "") + "</BenBic>";
                    xml_line += "<Description>" + bulkpayment.PaymentDescription.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</Description>";
                    //if (disbNum.Length < 6)
                    //{
                    //    disbNum = bulkpayment.BulkPaymentID.ToString().PadLeft(6, '0');
                    //}
                    xml_line += "<DisbNum>" + disbNum + "</DisbNum>";

                    xml_line += "<UnappliedAccount>" + PaymentBatchFile.UnappliedAccount + "</UnappliedAccount>";
                    xml_line += "<DetailsOfCharges>" + "SHA" + "</DetailsOfCharges>";

                    xml_line += "</TrxRecord>";
                    xml_text.Append(xml_line);

                }
                xml_line = "</BlockPayment>";
                /**** End of Payment Block ****/
                xml_text.Append(xml_line);
                xml_line = "</Document>";
                xml_text.Append(xml_line);
                File.WriteAllText(filePath, xml_text.ToString());//UNSIGNED XML FILE
                XMLValidator xmlValidationResponse = XMLTools.validateXMLFile(filePath, "Payment", strXMLSchemaPath);
                if (xmlValidationResponse.blnFileIsValid)
                {
                    string signedHashString = DigitalSignature.GenerateDigitalSignature(filePath, true, "EFT");
                    File.WriteAllText(strSignedFilePath, xml_text.ToString() + "|" + signedHashString); //SIGNED XML FILE
                    eftStatus.StringReturnValue = strSignedFilePath;
                    SystemConfig botUrlConfig = db.SystemConfigs.Where(a => a.ConfigName == "BOTUrl").FirstOrDefault();
                    if (botUrlConfig == null)
                    {
                        eftStatus.OverallStatus = "Error";
                        eftStatus.OverallStatusDescription = "XML schema configuration is incomplete. Please contact system administrator!";
                        return eftStatus;
                    }
                    string strBOTUrl = botUrlConfig.ConfigValue;
                    string sendStatus = "OK";
                    if (!debugMode)
                        sendStatus = SendToBOT(strSignedFilePath, "EFT", strBOTUrl);
                    if (sendStatus == "OK")
                    {
                        eftStatus.OverallStatus = "Success";
                    }
                    else
                    {

                        eftStatus.OverallStatus = "Error";
                        eftStatus.OverallStatusDescription = "Error Sending file(s) to BOT. Please contact Administrator";
                        return eftStatus;
                    }
                }
                else //Invalid XML file. 
                {
                    File.Move(filePath, strXMLErrorFilePath);
                    eftStatus.OverallStatus = "Invalid Tiss File";
                    eftStatus.OverallStatusDescription = string.Join(" *,* ", xmlValidationResponse.Errors);
                    System.IO.File.WriteAllText(strXMLErrorFilePath + "_Errorlog", eftStatus.OverallStatusDescription);//Log the XML errors for further action
                }

                return eftStatus;
            }
            catch (SqlException exSql)
            {
                eftStatus.OverallStatus = "DB Exception";
                eftStatus.OverallStatusDescription = "SQL Error:" + exSql.ToString();
                return eftStatus;
            }

        }

        public static ProcessResponse CreateBankTransferEFT(List<FundTransferDetail> TransferDetailsList, FundTransferSummary TransferSummary, IFMISTZDbContext db)
        {
            ProcessResponse eftStatus = new ProcessResponse();
            eftStatus.OverallStatus = "Pending";
            try
            {
                bool debugMode = false;

                SystemConfig eftConfig = db.SystemConfigs.Where(a => a.ConfigName == "EFTFilePath").FirstOrDefault();
                if (eftConfig == null)
                {
                    eftStatus.OverallStatus = "Error";
                    eftStatus.OverallStatusDescription = "EFT File Path configuration is incomplete. Please contact system administrator!";
                    return eftStatus;
                }
                string str_base_path = eftConfig.ConfigValue;

                SystemConfig xmlConfig = db.SystemConfigs.Where(a => a.ConfigName == "XMLSchemaPath").FirstOrDefault();
                if (xmlConfig == null)
                {
                    eftStatus.OverallStatus = "Error";
                    eftStatus.OverallStatusDescription = "XML schema configuration is incomplete. Please contact system administrator!";
                    return eftStatus;
                }
                string strXMLSchemaPath = xmlConfig.ConfigValue;

                string xml_line = "", filePath, strSignedFilePath, strXMLErrorFilePath;

                bool exists = System.IO.Directory.Exists(str_base_path);



                if (!exists)
                    System.IO.Directory.CreateDirectory(str_base_path);
                //Get payment totals
                //int NbOfTxs = (int)TransferSummary.NoTrx;
                decimal TotalAmount = (decimal)TransferSummary.TotalOperationalAmount;
                //MessageId
                int financialyear = ServiceManager.GetFinancialYear(db, DateTime.Now);
                //string MsgId = "MUST" + financialyear.ToString() + TransferSummary.TransferRefNum.ToString().PadLeft(2, '0') + TransferSummary.FundTransferSummaryId.ToString().PadLeft(8, '0');
                string MsgId = TransferSummary.MsgID;

                FundTransferSummary transferSummar = db.FundTransferSummaries.Find(TransferSummary.FundTransferSummaryId);
                Account acc = db.Accounts.Where(a => a.AccountNo == transferSummar.BankAccountFrom).FirstOrDefault();


                filePath = str_base_path + MsgId + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xml";
                strSignedFilePath = str_base_path + MsgId + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-SIGNED.xml";
                strXMLErrorFilePath = str_base_path + MsgId + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-INVALID.xml";
                filePath = str_base_path + "\\MUST-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + MsgId + ".xml";
                strSignedFilePath = str_base_path + "\\MUST-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + MsgId + "-SIGNED.xml";
                strXMLErrorFilePath = str_base_path + "\\MUST-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + MsgId + "-INVALID.xml";
                var xml_text = new StringBuilder();
                xml_line = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
                xml_line += "<Document xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"schema_transfer.xsd\">";
                xml_line += "<Header>";

                xml_line += "<Sender>MOFPTZTZ</Sender>";
                xml_line += "<Receiver>TANZTZTX</Receiver>";
                xml_line += "<MsgId>" + MsgId + "</MsgId>";
                xml_line += "<PaymentType>" + "P107" + "</PaymentType>";
                xml_line += "<MessageType>Transfer</MessageType>";

                xml_line += "</Header>";

                /*** Begin Payment Block ****/

                xml_line += "<MsgSummary>";

                xml_line += "<PaymentRef>" + "TRN" + TransferSummary.FundTransferSummaryId + "</PaymentRef>";
                xml_line += "<CreDtTm>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</CreDtTm>";
                xml_line += "<NbOfTxs>" + "1" + "</NbOfTxs>";
                xml_line += "<Currency>TZS</Currency>";
                xml_line += "<TotalAmount>" + TotalAmount.ToString("0.00") + "</TotalAmount>";
                xml_line += "<PayerName>" + acc.AccountName + "</PayerName>";
                xml_line += "<PayerAcct>" + acc.AccountNo + "</PayerAcct>";
                xml_line += "<RegionCode>" + "TZDO" + "</RegionCode>";
                xml_line += "<PaymentOffice>" + TransferSummary.InstitutionNameFrom + "</PaymentOffice>";

                xml_line += "</MsgSummary>";


                xml_text.Append(xml_line);

                Account account = db.Accounts.Where(a => a.AccountNo == TransferSummary.BankAccountTo && a.Status != "CANCELLED").FirstOrDefault();

                xml_line = "<TrxRecord>";
                xml_line += "<TransRef>" + TransferSummary.TransferRefNum + "</TransRef>";

                xml_line += "<RelatedRef>" + TransferSummary.TransferRefNum + "</RelatedRef>";
                xml_line += "<TransferType>" + "Deposit" + "</TransferType>";
                xml_line += "<TrxAmount>" + ((decimal)TransferSummary.TotalOperationalAmount).ToString("0.00") + "</TrxAmount>";
                xml_line += "<BenName>" + account.AccountName.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenName>";
                xml_line += "<BenAcct>" + account.AccountNo.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</BenAcct>";
                xml_line += "<Description>" + TransferSummary.TransferDescription.Replace("/", "").Replace("-", " ").Replace(".", "").Replace(";", "").Replace(",", "").Replace("&", "").Replace("+", "").Replace("'", "").Replace("-", " ").Replace(".", "").Replace("''", "").Replace("/", "").Replace("\\", "").Replace(",", "").Replace("`", "").Replace("\"", "").Replace("*", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace(";", "").Replace("ó", "o").Replace("ö", "o").Replace("á", "a").Replace("?", "").Replace("$", "").Replace("{", "").Replace("}", "").Replace("%", "") + "</Description>";

                xml_line += "</TrxRecord>";
                xml_text.Append(xml_line);

                /**** End of Payment Block ****/

                xml_line = "</Document>";
                xml_text.Append(xml_line);
                File.WriteAllText(filePath, xml_text.ToString());//UNSIGNED XML FILE
                XMLValidator xmlValidationResponse = XMLTools.validateXMLFile(filePath, "Transfer", strXMLSchemaPath);
                if (xmlValidationResponse.blnFileIsValid)
                {
                    string signedHashString = DigitalSignature.GenerateDigitalSignature(filePath, true, "EFT");
                    File.WriteAllText(strSignedFilePath, xml_text.ToString() + "|" + signedHashString); //SIGNED XML FILE
                    eftStatus.StringReturnValue = strSignedFilePath;
                    SystemConfig botUrlConfig = db.SystemConfigs.Where(a => a.ConfigName == "BOTUrl").FirstOrDefault();
                    if (botUrlConfig == null)
                    {
                        eftStatus.OverallStatus = "Error";
                        eftStatus.OverallStatusDescription = "XML schema configuration is incomplete. Please contact system administrator!";
                        return eftStatus;
                    }
                    string strBOTUrl = botUrlConfig.ConfigValue;
                    string sendStatus = "OK";
                    if (Properties.Settings.Default.HostingEnvironment == "Dev")
                    {
                        eftStatus.OverallStatus = "Success";
                        return eftStatus;
                    }
                    sendStatus = SendToBOT(strSignedFilePath, "EFT", strBOTUrl);
                    if (sendStatus == "OK")
                    {
                        eftStatus.OverallStatus = "Success";
                    }
                    else
                    {

                        eftStatus.OverallStatus = "Error";
                        eftStatus.OverallStatusDescription = "Error Sending file(s) to BOT. Please contact Administrator";
                        return eftStatus;
                    }
                }
                else //Invalid XML file. 
                {
                    File.Move(filePath, strXMLErrorFilePath);
                    eftStatus.OverallStatus = "Invalid Tiss File";
                    eftStatus.OverallStatusDescription = string.Join(" *,* ", xmlValidationResponse.Errors);
                    System.IO.File.WriteAllText(strXMLErrorFilePath + "_Errorlog", eftStatus.OverallStatusDescription);//Log the XML errors for further action
                }

                return eftStatus;
            }
            catch (SqlException exSql)
            {
                eftStatus.OverallStatus = "DB Exception";
                eftStatus.OverallStatusDescription = "SQL Error:" + exSql.ToString();
                return eftStatus;
            }

        }







        public static bool IsDateValid(List<DateTime> dates)
        {
            foreach (var date in dates)
            {
                if (date > DateTime.Now)
                {
                    return false;
                }
            }
            return true;
        }

        public static string sendBotPaymentSlip(string filePath, string msgId, string targetUrl)
        {
            /*** Sending the file using MultipartFormData ***/
            string responseStatus = "OK";
            Encoding encoding = Encoding.UTF8;
            try
            {
                string fileName = msgId + ".pdf";
                string fileType = "application/pdf";

                //Creating Request of the destination URL
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(targetUrl);
                /////

                if (httpRequest == null)
                {
                    throw new NullReferenceException("request is not a http request");
                }


                //Defining the type of the posted data multipart form with a boundary
                string boundary = "-------------------------acebdf13572468"; //String.Format("----------{0:N}", Guid.NewGuid());

                string contentType = "multipart/form-data; boundary=" + boundary;//"text/xml";//  "application/pdf";//
                // Set up the request properties.
                httpRequest.ContentType = contentType;
                httpRequest.UserAgent = "MOFP";
                httpRequest.CookieContainer = new CookieContainer();
                httpRequest.Method = "POST";
                //////////////////////////////////////////////////////////////////////

                Stream formDataStream = new System.IO.MemoryStream();
                // string strFormData = "Content-Type: " + contentType + Environment.NewLine;
                //strFormData += "User-Agent: " + httpRequest.UserAgent + Environment.NewLine;
                //////

                // Add just the first part of this param, since we will write the file data directly to the Stream
                string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                    boundary,
                    "file",
                    fileName,
                   fileType); //"application/octet-stream"
                              //   strFormData += header;
                formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                // Write the file data directly to the Stream, rather than serializing it to a string.
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                byte[] fileData = new byte[fs.Length];
                fs.Read(fileData, 0, fileData.Length);
                fs.Close();
                formDataStream.Write(fileData, 0, fileData.Length);
                /////
                // strFormData += @"<@INCLUDE *" + filePath  +"*@>";

                // Add the end of the request.  Start with a newline
                string footer = "\r\n--" + boundary + "--\r\n";
                formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));
                //  strFormData += footer;
                // Dump the Stream into a byte[]
                formDataStream.Position = 0;
                byte[] formData = new byte[formDataStream.Length];
                formDataStream.Read(formData, 0, formData.Length);
                formDataStream.Close();


                //////////////////////////////////////////////////////////////////////////
                httpRequest.ContentLength = formData.Length;
                // Get the request stream.
                Stream requestStream = httpRequest.GetRequestStream();
                // Write the data to the request stream.
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
                //Get Response
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                string strResponse = "";

                //string strResponse = "Status Code: " + httpResponse.StatusCode.ToString() + Environment.NewLine
                //    + "Status Description: " + httpResponse.StatusDescription.ToString() + Environment.NewLine
                //    + "ResponseUri:" + httpResponse.ResponseUri.ToString() + Environment.NewLine
                //    + "Server:" + httpResponse.Server.ToString() + Environment.NewLine + Environment.NewLine
                //    + "Headers:" + httpResponse.Headers.ToString();
                //Log the http response from target.
                File.WriteAllText(filePath + "_Response", strResponse);
                Exception info = new Exception(strResponse);
                ErrorSignal.FromCurrentContext().Raise(info);
                responseStatus = httpResponse.StatusCode.ToString();

            }
            catch (Exception ex)
            {
                responseStatus = ex.Message.ToString();
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return responseStatus;
        }

        public static ProcessResponse UpdateGLQueue(IFMISTZDbContext db, string journalCode)
        {
            ProcessResponse glUpdateStatus = new ProcessResponse();
            glUpdateStatus.OverallStatus = "Pending";
            string[] aPgeneralJournalCodes = { "PV", "PD", "IV", "PO", "PRI", "PRR" };
            string[] aRgeneralJournalCodes = { "IM", "IRF", "IRR", "IRP", "LNI", "LNR" };
            string[] rRgeneralJournalCodes = { "RV", "RR", "OS" };
            string[] cMgeneralJournalCodes = { "WR", "FTF", "FTI", "FTR", "DR", "DP" };
            string[] gLgeneralJournalCodes = { "GJ" };

            if (aPgeneralJournalCodes.Contains(journalCode))
            {
                BackgroundJob.Enqueue(() => SpApGeneralLedgerUpdate(db, journalCode));
            }
            else if (aRgeneralJournalCodes.Contains(journalCode))
            {
                BackgroundJob.Enqueue(() => SpArGeneralLedgerUpdate(db, journalCode));
            }
            else if (rRgeneralJournalCodes.Contains(journalCode))
            {
                BackgroundJob.Enqueue(() => SpRrGeneralLedgerUpdate(db, journalCode));
            }
            else if (cMgeneralJournalCodes.Contains(journalCode))
            {
                BackgroundJob.Enqueue(() => SpCmGeneralLedgerUpdate(db, journalCode));
            }
            else if (gLgeneralJournalCodes.Contains(journalCode))
            {
                BackgroundJob.Enqueue(() => SpGlGeneralLedgerUpdate(db, journalCode));
            }

            return glUpdateStatus;
        }

        public static void SpApGeneralLedgerUpdate(IFMISTZDbContext db, string journalCode)
        {
            var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalCode) };
            db.Database.ExecuteSqlCommand("dbo.sp_APGeneralLedgerUpdate @JournalTypeCode", parameters);
        }

        public static void SpArGeneralLedgerUpdate(IFMISTZDbContext db, string journalCode)
        {
            var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalCode) };
            db.Database.ExecuteSqlCommand("dbo.sp_ARGeneralLedgerUpdate @JournalTypeCode", parameters);
        }

        public static void SpRrGeneralLedgerUpdate(IFMISTZDbContext db, string journalCode)
        {
            var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalCode) };
            db.Database.ExecuteSqlCommand("dbo.sp_RRGeneralLedgerUpdate @JournalTypeCode", parameters);
        }

        public static void SpCmGeneralLedgerUpdate(IFMISTZDbContext db, string journalCode)
        {
            var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalCode) };
            db.Database.ExecuteSqlCommand("dbo.sp_CMGeneralLedgerUpdate @JournalTypeCode", parameters);
        }

        public static void SpGlGeneralLedgerUpdate(IFMISTZDbContext db, string journalCode)
        {
            var parameters = new SqlParameter[] { new SqlParameter("@JournalTypeCode", journalCode) };
            db.Database.ExecuteSqlCommand("dbo.sp_GLGeneralLedgerUpdate @JournalTypeCode", parameters);
        }

        public static string ToBase64String(this Bitmap bmp, ImageFormat imageFormat)
        {
            string base64String = string.Empty;

            MemoryStream memoryStream = new MemoryStream();
            bmp.Save(memoryStream, imageFormat);

            memoryStream.Position = 0;
            byte[] byteBuffer = memoryStream.ToArray();

            memoryStream.Close();

            base64String = Convert.ToBase64String(byteBuffer);
            byteBuffer = null;

            return base64String;
        }

        public static ProcessResponse generateCoa(IFMISTZDbContext db, string glAccount, System.Security.Principal.IPrincipal loggedInUser)
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

                Institution institution = db.Institution.Where(a => a.VoteCode == vote && a.Level2Code == trCode && a.Level3Code == geographicalLocation).FirstOrDefault();

                if (facilityCode == "00000000") // No facility
                {
                    institution = db.Institution.Where(a => a.VoteCode == vote
                    && a.Level2Code == trCode
                    && a.Level3Code == geographicalLocation
                    ).FirstOrDefault();
                    if (institution == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "Institution Setup does not match COA segment setup for vote '"
                            + vote + "', TR '" + trCode + "' and geographical location '"
                            + geographicalLocation + "'.";
                        return coaStatus;
                    }
                }
                else
                {
                    institution = db.Institution.Where(a => a.VoteCode == vote
                                       && a.Level2Code == trCode
                                       && a.Level3Code == geographicalLocation
                                       && a.Level4Code == facilityCode
                                       ).FirstOrDefault();
                    if (institution == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "Institution Setup does not match COA segment setup for vote '"
                            + vote + "', TR '" + trCode + "' and geographical location '"
                            + geographicalLocation
                            + " and facility code " + facilityCode
                            + "'.";
                        return coaStatus;
                    }
                }


                int InstitutionId = institution.InstitutionId;

                if (db.COAs.Where(a => a.GlAccount == glAccount && a.InstitutionId == InstitutionId && a.Status.ToUpper() != "CANCELLED").Count() > 0)
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

                if (CoaCheck(subVoteCode, 2, db) == 0)
                {
                    coaStatus.OverallStatus = "Error";
                    coaStatus.OverallStatusDescription = "SubVote Does not Exist: " + subVoteCode;
                    return coaStatus;
                }

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
                var activeCoaVersion = db.CoaVersions.Where(a => a.OverallStatus.ToUpper() == "ACTIVE").FirstOrDefault();

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


                string subVoteDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == subVoteCode
                    && a.SegmentName == "SUB VOTE").FirstOrDefault().SegmentDesc;
                //TODO: Review sub level description for Ministries and other Main Votes
                string costCentreDesc = "N/A";
                if (costCentreCode != "0000")
                {
                    InstitutionSubLevel subLevel = db.InstitutionSubLevels
                                                    .Where(a => a.InstitutionCode == institution.InstitutionCode
                                                    && a.SubLevelCategory == "COST CENTRE"
                                                    && a.SubLevelCode == costCentreCode).FirstOrDefault();
                    if (subLevel == null)
                    {
                        coaStatus.OverallStatus = "Error";
                        coaStatus.OverallStatusDescription = "Error adding gl account '" + glAccount + "'. Paystation for institutionCode '" + institution.InstitutionCode + "' and cost Centre '" + costCentreCode + "' does not exist.";
                        return coaStatus;
                    }
                    costCentreDesc = subLevel.SubLevelDesc;
                }


                string facilityDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == facilityCode
                    && a.SegmentName == "FACILITY").FirstOrDefault().SegmentDesc;

                string gfsDesc = db.CoaSegments.
                    Where(a => a.SegmentCode == gfsCode
                    && a.SegmentName == "GFS CODE").FirstOrDefault().SegmentDesc;


                db.COAs.Add(new COA
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
                    GlAccount = glAccount,
                    GlAccountDesc = gfsDesc,
                    CreatedBy = loggedInUser.Identity.Name,
                    CreatedAt = System.DateTime.Now,
                    InstitutionId = institution.InstitutionId,
                    Status = "ACTIVE"


                });

                db.SaveChanges();

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

        public static string DecimalToText(decimal d)
        {
            //Grab a string form of your decimal value ("12.34")
            var formatted = d.ToString();

            if (formatted.Contains("."))
            {
                //If it contains a decimal point, split it into both sides of the decimal
                string[] sides = formatted.Split('.');

                //Process each side and append them with "and", "dot" or "point" etc.
                if (NumberToText(int.Parse(sides[1]), true) == "Zero")
                {
                    return NumberToText(int.Parse(sides[0]), true);
                }
                return NumberToText(int.Parse(sides[0]), true) + " and " + NumberToText(int.Parse(sides[1]), true);
            }
            else
            {
                //Else process as normal
                return NumberToText(Convert.ToInt32(d), true);
            }
        }


        public static BalanceResponse GetBudgetBalance(IFMISTZDbContext db, string institutionCode, string subBudgetClass, string subVoteCostCentre)
        {
            BalanceResponse balanceResponse = new BalanceResponse();
            balanceResponse.overallStatus = "Pending";

            var conn = db.Database.Connection;
            try
            {

                string procedureName = "sp_GetBudgetBalance";
                conn.Open();
                using (var command = conn.CreateCommand())
                {

                    command.CommandText = procedureName;
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter institutionParam = new SqlParameter("@InstitutionCode", institutionCode);
                    institutionParam.Direction = ParameterDirection.Input;
                    institutionParam.DbType = DbType.String;
                    command.Parameters.Add(institutionParam);
                    SqlParameter sbcParam = new SqlParameter("@SubBudgetClass", subBudgetClass);
                    sbcParam.Direction = ParameterDirection.Input;
                    sbcParam.DbType = DbType.String;
                    command.Parameters.Add(sbcParam);
                    SqlParameter sbcSubVoteCostCentre = new SqlParameter("@SubVote", subVoteCostCentre);
                    sbcSubVoteCostCentre.Direction = ParameterDirection.Input;
                    sbcSubVoteCostCentre.DbType = DbType.String;
                    command.Parameters.Add(sbcSubVoteCostCentre);

                    DbDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        balanceResponse.BudgetBalanceViewList = new List<BudgetBalanceView>();

                        while (reader.Read())
                        {
                            BudgetBalanceView budgetBalanceView = new BudgetBalanceView
                            {
                                FundBalanceViewId = (long)reader["FundBalanceViewId"],
                                BudgetOperationalAmount = (decimal)reader["BudgetOperationalAmount"],
                                BudgetBaseAmount = (decimal)reader["BudgetBaseAmount"],
                                FundingSource = "",
                                Subvote = reader["Subvote"].ToString(),
                                GlAccount = reader["GlAccount"].ToString(),
                                GlAccountDesc = reader["GlAccountDesc"].ToString(),
                                InstitutionCode = reader["InstitutionCode"].ToString(),
                                SourceModule = reader["SourceModule"].ToString(),
                                JournalTypeCode = reader["JournalTypeCode"].ToString(),
                                SubLevelCategory = reader["SubLevelCategory"].ToString(),
                                SublevelCode = reader["SublevelCode"].ToString(),
                                SubBudgetClass = reader["SubBudgetClass"].ToString(),
                                TrxType = reader["TrxType"].ToString(),
                                DrGfsCode = reader["DrGfsCode"].ToString(),
                                AllocationAmount = (decimal)reader["AllocationAmount"],
                                ExpenditureToDate = (decimal)reader["ExpenditureToDate"],
                                BudgetBalance = (decimal)reader["BudgetBalance"],

                            };

                            balanceResponse.BudgetBalanceViewList.Add(budgetBalanceView);
                        }
                    }
                    reader.Dispose();
                }
            }
            catch (Exception ex)
            {
                balanceResponse.overallStatus = "Error";
                balanceResponse.overallStatusDescription = "Error getting budget balance. " + ex.Message.ToString();
                conn.Close();
            }
            conn.Close();
            return balanceResponse;
        }

        public static BalanceResponse GetBudgetBalanceNoSubVote(IFMISTZDbContext db, string institutionCode, string subBudgetClass)
        {
            BalanceResponse balanceResponse = new BalanceResponse();
            balanceResponse.overallStatus = "Pending";

            var conn = db.Database.Connection;
            try
            {

                string procedureName = "sp_GetBudgetBalanceNoSubVote";
                conn.Open();
                using (var command = conn.CreateCommand())
                {

                    command.CommandText = procedureName;
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter institutionParam = new SqlParameter("@InstitutionCode", institutionCode);
                    institutionParam.Direction = ParameterDirection.Input;
                    institutionParam.DbType = DbType.String;
                    command.Parameters.Add(institutionParam);
                    SqlParameter sbcParam = new SqlParameter("@SubBudgetClass", subBudgetClass);
                    sbcParam.Direction = ParameterDirection.Input;
                    sbcParam.DbType = DbType.String;
                    command.Parameters.Add(sbcParam);


                    DbDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        balanceResponse.BudgetBalanceViewList = new List<BudgetBalanceView>();

                        while (reader.Read())
                        {
                            BudgetBalanceView budgetBalanceView = new BudgetBalanceView
                            {
                                FundBalanceViewId = (long)reader["FundBalanceViewId"],
                                BudgetOperationalAmount = (decimal)reader["BudgetOperationalAmount"],
                                BudgetBaseAmount = (decimal)reader["BudgetBaseAmount"],
                                FundingSource = "",
                                Subvote = reader["Subvote"].ToString(),
                                GlAccount = reader["GlAccount"].ToString(),
                                GlAccountDesc = reader["GlAccountDesc"].ToString(),
                                InstitutionCode = reader["InstitutionCode"].ToString(),
                                SourceModule = reader["SourceModule"].ToString(),
                                JournalTypeCode = reader["JournalTypeCode"].ToString(),
                                SubLevelCategory = reader["SubLevelCategory"].ToString(),
                                SublevelCode = reader["SublevelCode"].ToString(),
                                SubBudgetClass = reader["SubBudgetClass"].ToString(),
                                TrxType = reader["TrxType"].ToString(),
                                DrGfsCode = reader["DrGfsCode"].ToString(),
                                AllocationAmount = (decimal)reader["AllocationAmount"],
                                ExpenditureToDate = (decimal)reader["ExpenditureToDate"],
                                BudgetBalance = (decimal)reader["BudgetBalance"],

                            };

                            balanceResponse.BudgetBalanceViewList.Add(budgetBalanceView);
                        }
                    }
                    reader.Dispose();
                }
            }
            catch (Exception ex)
            {
                balanceResponse.overallStatus = "Error";
                balanceResponse.overallStatusDescription = "Error getting budget balance. " + ex.Message.ToString();
                conn.Close();
            }
            conn.Close();
            return balanceResponse;
        }

        public static BalanceResponse GetFundBalance(IFMISTZDbContext db, string institutionCode, string subBudgetClass)
        {

            BalanceResponse balanceResponse = new BalanceResponse();
            balanceResponse.overallStatus = "Pending";

            var conn = db.Database.Connection;
            try
            {

                string procedureName = "sp_GetFundBalance";
                conn.Open();
                using (var command = conn.CreateCommand())
                {

                    command.CommandText = procedureName;
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter institutionParam = new SqlParameter("@InstitutionCode", institutionCode);
                    institutionParam.Direction = ParameterDirection.Input;
                    institutionParam.DbType = DbType.String;
                    command.Parameters.Add(institutionParam);
                    SqlParameter sbcParam = new SqlParameter("@SubBudgetClass", subBudgetClass);
                    sbcParam.Direction = ParameterDirection.Input;
                    sbcParam.DbType = DbType.String;
                    command.Parameters.Add(sbcParam);

                    DbDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        balanceResponse.FundBalanceViewList = new List<FundBalanceView>();

                        while (reader.Read())
                        {
                            FundBalanceView fundBalanceView = new FundBalanceView
                            {
                                FundBalanceViewId = (long)reader["FundBalanceViewId"],
                                BudgetOperationalAmount = (decimal)reader["BudgetOperationalAmount"],
                                BudgetBaseAmount = (decimal)reader["BudgetBaseAmount"],
                                FundingRefNo = reader["FundingRefNo"].ToString(),
                                FundingSource = reader["FundingSource"].ToString(),
                                GlAccount = reader["GlAccount"].ToString(),
                                GlAccountDesc = reader["GlAccountDesc"].ToString(),
                                InstitutionCode = reader["InstitutionCode"].ToString(),
                                SourceModule = reader["SourceModule"].ToString(),
                                JournalTypeCode = reader["JournalTypeCode"].ToString(),
                                SubLevelCategory = reader["SubLevelCategory"].ToString(),
                                SublevelCode = reader["SublevelCode"].ToString(),
                                SubBudgetClass = reader["SubBudgetClass"].ToString(),
                                TrxType = reader["TrxType"].ToString(),
                                DrGfsCode = reader["DrGfsCode"].ToString(),
                                AllocationAmount = (decimal)reader["AllocationAmount"],
                                ExpenditureToDate = (decimal)reader["ExpenditureToDate"],
                                FundBalance = (decimal)reader["FundBalance"],
                            };

                            balanceResponse.FundBalanceViewList.Add(fundBalanceView);
                        }
                    }
                    reader.Dispose();
                }
            }
            catch (Exception ex)
            {
                balanceResponse.overallStatus = "Error";
                balanceResponse.overallStatusDescription = "Error getting budget balance. " + ex.Message.ToString();
                conn.Close();
            }
            conn.Close();
            return balanceResponse;
        }
    }
}