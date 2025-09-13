using Elmah;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using Hangfire;
using Serilog;
using System.Data;
using IFMIS.DAL;
using IFMIS.Models;
using IFMIS.Libraries;
using IFMIS.Areas.IFMISTZ.Models;

namespace IFMIS.Controllers
{
    public class BankingServicesController : Controller
    {
        private readonly IFMISTZDbContext db = new IFMISTZDbContext();

        [HttpPost]
        public string Index()
        {
            var response = "";
            var data = "";
            var dataSignature = "";
            var respStatus = "ACCEPTED";
            var statusDesc = "Accepted Succesfully";
            var schemaPath = "";
            var defaultSender = "MOFPTZTZ";
            var defaultReceiver = "MOFPTZTZ";
            var defaultMsgId = "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var defaultPaymentType = "P100";
            var defaultMessageType = "RESPONSE";
            var inputStream = "";
            string certPass = "";
            string certStorePath = "";

            try
            {
                inputStream = new StreamReader(HttpContext.Request.InputStream).ReadToEnd().ToString();

                //Log request
                Log.Information(inputStream + "{Name}!", "IncomingMessages");

                var inputStreamArray = inputStream.Split('|');
                data = inputStreamArray[0];
                dataSignature = inputStreamArray[1];
                var xDoc = XDocument.Parse(data);
                Log.Information(xDoc + "{Name}!", "xDocIncomingMessages");
                var headerVM = (from x in xDoc.Descendants("Header")
                                select new BankingHeaderVM
                                {
                                    Sender = (string)x.Element("Sender"),
                                    Receiver = (string)x.Element("Receiver"),
                                    MsgId = (string)x.Element("MsgId"),
                                    PaymentType = (string)x.Element("PaymentType"),
                                    MessageType = (string)x.Element("MessageType"),
                                }).FirstOrDefault();

                if (headerVM == null)
                {
                    headerVM.Sender = defaultSender;
                    headerVM.Receiver = defaultReceiver;
                    headerVM.MsgId = defaultMsgId;
                    headerVM.PaymentType = defaultPaymentType;
                    headerVM.MessageType = defaultMessageType;
                    respStatus = "REJECTED";
                    statusDesc = "BAD REQUEST";

                    response = GetResponse(headerVM, respStatus, statusDesc);
                    Log.Information(response + "{Name}!", "OutgoingResponseBadRequests");
                    //Log.CloseAndFlush();
                    return response;
                }

                //Check schema
                if (headerVM.MessageType.ToUpper() == "SETTLEMENT")
                {
                    schemaPath = Properties.Settings.Default.schemaFilePath + "schema_settlement.xsd";
                }
                else if (headerVM.MessageType.ToUpper() == "UNAPPLIED")
                {
                    schemaPath = Properties.Settings.Default.schemaFilePath + "schema_unapplied.xsd";
                }
                else if (headerVM.MessageType.ToUpper() == "STATEMENT")
                {
                    schemaPath = Properties.Settings.Default.schemaFilePath + "schema_bank_statement.xsd";
                }
                else if (headerVM.MessageType.ToUpper() == "RESPONSE")
                {
                    schemaPath = Properties.Settings.Default.schemaFilePath + "schema_block_response.xsd";
                }

                var validationResult = XMLTools.ValidateXml(xDoc, schemaPath);
                if (validationResult.HasErrors && Properties.Settings.Default.checkSchema)
                {
                    respStatus = "REJECTED";
                    statusDesc = validationResult.ValidationDesc;
                    response = GetResponse(headerVM, respStatus, statusDesc);
                    Log.Information(inputStream + "{Name}!", "IncomingMessageInvalidSchemas");
                    return response;
                }

                //Check signature
                var apiClient = db.ApiClients.Where(a => a.ClientId == headerVM.Sender).FirstOrDefault();
                if (apiClient == null)
                {
                    respStatus = "REJECTED";
                    statusDesc = "INVALID SENDER";
                    response = GetResponse(headerVM, respStatus, statusDesc);
                    Log.Information(inputStream + "{Name}!", "IncomingMessageInvalidSender");
                    return response;
                }

                certPass = apiClient.ClientPassword;
                certStorePath = apiClient.ClientPublicKey;

                var isSignatureValid = DigitalSignature.VerifySignature(certStorePath, certPass, data, dataSignature);
                if (!isSignatureValid && Properties.Settings.Default.checkSignature)
                {
                    respStatus = "REJECTED";
                    statusDesc = "INVALID SIGNATURE";
                    response = GetResponse(headerVM, respStatus, statusDesc);
                    Log.Information(inputStream + "{Name}!", "IncomingMessageInvalidSignatures");
                    return response;
                }

                //Check Duplicate
                var isDuplicate = false;
                isDuplicate = db.IncomingMessages.Where(a => a.MsgID == headerVM.MsgId).Any();
                if (isDuplicate)
                {
                    respStatus = "REJECTED";
                    statusDesc = "DUPLICATE MsgId " + headerVM.MsgId;
                    response = GetResponse(headerVM, respStatus, statusDesc);
                    Log.Information(inputStream + "{Name}!", "IncomingMessageDuplicates");
                    return response;
                }

                if (headerVM.MessageType.ToUpper() == "STATEMENT")
                {
                    var bankStatementSummary = (from x in xDoc.Descendants("MsgSummary")
                                                select new BankStatementSummary
                                                {
                                                    IncomingMessageId = 0,
                                                    BankAccountNumber = (string)x.Element("AcctNum"),
                                                    StatementDate = (DateTime)x.Element("SmtDt"),
                                                }).FirstOrDefault();

                    isDuplicate = db.BankStatementSummarys.Where(a => a.BankStatementInternalReference == bankStatementSummary.GetBankStatementInternalRef).Any();
                    if (isDuplicate)
                    {
                        respStatus = "REJECTED";
                        statusDesc = "DUPLICATE BANK STATEMENT FOR THE ACCOUNT NO "
                        + bankStatementSummary.BankAccountNumber
                        + " ON " + bankStatementSummary.StatementDate;
                        response = GetResponse(headerVM, respStatus, statusDesc);
                        Log.Information(inputStream + "{Name}!", "IncomingDuplicateBankStatements1");
                        return response;
                    }
                }

                //put in q
                Log.Information("Start Hangfire" + "{Name}!", "StartHangFire");
                BackgroundJob.Enqueue(() => SavePayLoad(headerVM, xDoc, inputStream));
                response = GetResponse(headerVM, respStatus, statusDesc);
                Log.Information(response + "{Name}!", "OutgoingResponses");
            }
            catch (Exception ex)
            {
                Log.Information(ex + "{Name}!", "FirstIncomingMessageExceptions");
                if (System.Web.HttpContext.Current != null)
                {
                    ErrorSignal.FromCurrentContext().Raise(ex);
                }
                else
                {
                    ErrorLog.GetDefault(null).Log(new Error(ex));
                }

                var headerVM = new BankingHeaderVM
                {
                    Sender = defaultSender,
                    Receiver = defaultReceiver,
                    MsgId = defaultMsgId,
                    PaymentType = defaultPaymentType,
                    MessageType = defaultMessageType,
                };

                respStatus = "REJECTED";
                statusDesc = "BAD REQUEST," + ex.Message;
                response = GetResponse(headerVM, respStatus, statusDesc);
                Log.Information(inputStream + "{Name}!", "SecondIncomingMessageExceptions");
                return response;
            }

            return response;
        }

        [Queue("save_incoming_bank_queue")]
        public void SavePayLoad(BankingHeaderVM headerVM, XDocument xDoc, string inputStream)
        {
            var countMsgId = db.IncomingMessages.Where(a => a.MsgID == headerVM.MsgId).Any();

            if (!countMsgId)
            {
                var incomingMessage = new IncomingMessage
                {
                    MsgID = headerVM.MsgId,
                    //PaymentRef = paymentRef,
                    PaymentType = headerVM.PaymentType,
                    //messageProcessStatus = "ACCEPTED",
                    messageProcessStatusDescription = "Valid XML",
                    MessageType = headerVM.MessageType,
                    //OriginalMsgID = headerVM.orgMsgId,
                    MessageTimeStamp = DateTime.Now,
                    DatabaseUpdateStatus = "Automatic Updated",
                    //BotResponse = respStatus,
                    //BotResponseDescription = desc,
                    //PaymentUpdateStatus = "Pending",
                    XmlContent = inputStream
                };

                db.IncomingMessages.Add(incomingMessage);

                if (headerVM.MessageType.ToUpper() == "SETTLEMENT")
                {
                    var incomingMessageDetails = (from x in xDoc.Descendants("TrxRecord")
                                                  select new IncomingMessageDetail
                                                  {
                                                      IncomingMessageID = incomingMessage.ID,
                                                      OrgEndToEndId = (string)x.Element("OrgEndToEndId"),
                                                      PayChannel = (string)x.Element("PayChannel"),
                                                      ValueDate = (string)x.Element("ValueDate"),
                                                      Reference = (string)x.Element("Reference"),
                                                      DatabaseUpdateStatus = "Automatic Updated",
                                                      MessageTimeStamp = DateTime.Now,
                                                      TransactionStatus = "SETTLED",
                                                      TransactionStatusCode = "SETTLED",
                                                      TransactionStatusDescription = "Settled through " + (string)x.Element("PayChannel") + " on " + (string)x.Element("ValueDate") + " with Banking Reference " + (string)x.Element("Reference")
                                                  }).ToList();

                    db.IncomingMessageDetails.AddRange(incomingMessageDetails);

                    foreach (var item in incomingMessageDetails)
                    {
                        if (headerVM.PaymentType == "P108")
                        {
                            var paymentVoucher = db.PaymentVouchers.Where(a => a.PVNo == item.OrgEndToEndId).FirstOrDefault();
                            if (paymentVoucher != null)
                            {
                                paymentVoucher.OverallStatus = item.TransactionStatus;
                                paymentVoucher.OverallStatusDesc = item.TransactionStatusDescription;
                            }
                        }
                        else if (headerVM.PaymentType == "P120")
                        {
                            var bulkPayment = db.BulkPayments.Where(a => a.EndtoEndID == item.OrgEndToEndId).FirstOrDefault();
                            if (bulkPayment != null)
                            {
                                bulkPayment.OverallStatus = item.TransactionStatus;
                                bulkPayment.SettledDescription = item.TransactionStatusDescription;
                                bulkPayment.SettledAt = DateTime.Now;
                            }
                        }
                        else if (headerVM.PaymentType == "P500")
                        {
                            var transfer = db.FundTransferSummaries.Where(a => a.TransferRefNum == item.OrgEndToEndId).FirstOrDefault();
                            if (transfer != null)
                            {
                                transfer.OverallStatus = item.TransactionStatus;
                                transfer.OverallStatus = "PROCESSED";
                                //transfer.SettledDescription = item.TransactionStatusDescription;
                                //transfer.SettledAt = DateTime.Now;
                            }
                        }
                    }
                }
                else if (headerVM.MessageType.ToUpper() == "UNAPPLIED")
                {
                    var incomingMessageDetails = (from x in xDoc.Descendants("TrxRecord")
                                                  select new IncomingMessageDetail
                                                  {
                                                      IncomingMessageID = incomingMessage.ID,
                                                      OrgEndToEndId = (string)x.Element("OrgEndToEndId"),
                                                      DatabaseUpdateStatus = "Automatic Updated",
                                                      MessageTimeStamp = DateTime.Now,
                                                      TransactionStatus = "UNAPPLIED",
                                                      TransactionStatusCode = (string)x.Element("ReturnCode"),
                                                      TransactionStatusDescription = (string)x.Element("Description")
                                                  }).ToList();

                    db.IncomingMessageDetails.AddRange(incomingMessageDetails);

                    foreach (var item in incomingMessageDetails)
                    {
                        if (headerVM.PaymentType == "P108")
                        {
                            var paymentVoucher = db.PaymentVouchers.Where(a => a.PVNo == item.OrgEndToEndId).FirstOrDefault();
                            if (paymentVoucher != null)
                            {
                                paymentVoucher.OverallStatus = item.TransactionStatus;
                                paymentVoucher.OverallStatusDesc = item.TransactionStatusDescription;
                            }
                        }
                        else if (headerVM.PaymentType == "P120")
                        {
                            var bulkPayment = db.BulkPayments.Where(a => a.EndtoEndID == item.OrgEndToEndId).FirstOrDefault();
                            if (bulkPayment != null)
                            {
                                bulkPayment.OverallStatus = item.TransactionStatus;
                                bulkPayment.SettledDescription = item.TransactionStatusDescription;
                                bulkPayment.SettledAt = DateTime.Now;
                            }
                        }
                        else if (headerVM.PaymentType == "P500")
                        {
                            var transfer = db.FundTransferSummaries.Where(a => a.TransferRefNum == item.OrgEndToEndId).FirstOrDefault();
                            if (transfer != null)
                            {
                                transfer.OverallStatus = item.TransactionStatus;
                                //transfer.SettledDescription = item.TransactionStatusDescription;
                                //transfer.SettledAt = DateTime.Now;
                            }
                        }
                    }
                }
                else if (headerVM.MessageType.ToUpper() == "STATEMENT")
                {
                    var bankName = "";
                    var bank = db.Banks.Where(a => a.BIC == headerVM.Sender).FirstOrDefault();
                    if (bank != null)
                    {
                        bankName = bank.BankName;
                    }
                    var bankStatementSummary = (from x in xDoc.Descendants("MsgSummary")
                                                select new BankStatementSummary
                                                {
                                                    IncomingMessageId = incomingMessage.ID,
                                                    BankName = bankName,
                                                    BankBic = headerVM.Sender,
                                                    BankAccountName = (string)x.Element("AcctName"),
                                                    BankAccountNumber = (string)x.Element("AcctNum"),
                                                    CurrencyCode = (string)x.Element("Currency"),
                                                    CreatedDateTime = (DateTime)x.Element("CreDtTm"),
                                                    StatementDate = (DateTime)x.Element("SmtDt"),
                                                    OpenCdtDbtInd = (string)x.Element("OpenCdtDbtInd"),
                                                    OpeningBalance = (decimal)x.Element("OpenBal"),
                                                    CloseCdtDbtInd = (string)x.Element("CloseCdtDbtInd"),
                                                    ClosingBalance = (decimal)x.Element("CloseBal")
                                                }).FirstOrDefault();

                    var checkBankStatementInternalRef = db.BankStatementSummarys.Where(a => a.BankStatementInternalReference == bankStatementSummary.GetBankStatementInternalRef).Any();

                    if (checkBankStatementInternalRef)
                    {
                        Log.Information(inputStream + "{Name}!", "IncomingDuplicateBankStatements2");
                    }
                    else
                    {
                        bankStatementSummary.BankStatementInternalReference = bankStatementSummary.GetBankStatementInternalRef;
                        db.BankStatementSummarys.Add(bankStatementSummary);

                        var bankStatementDetails = (from x in xDoc.Descendants("TrxRecord")
                                                    select new BankStatementDetail
                                                    {
                                                        BankStatementSummaryId = bankStatementSummary.BankStatementSummaryId,
                                                        TransactionRef = (string)x.Element("BankRef"),
                                                        RelatedRef = (string)x.Element("RelatedRef"),
                                                        TransactionType = (string)x.Element("TranType"),
                                                        TransactionAmount = (decimal)x.Element("TrxAmount"),
                                                        TranCodeType = (string)x.Element("TranCode"),
                                                        Description = (string)x.Element("Description"),
                                                        ReconciliationStatus = "Pending"
                                                    }).ToList();

                        db.BankStatementDetails.AddRange(bankStatementDetails);
                    }
                }
                else if (headerVM.MessageType.ToUpper() == "RESPONSE")
                {
                    var orgMsgId = "";
                    var creDtTm = "";
                    var paymentRef = "";
                    var respStatus = "";
                    var desc = "";

                    var responseSummary = (from u in xDoc.Descendants("ResponseSummary")
                                           select new
                                           {
                                               OrgMsgId = (string)u.Element("OrgMsgId"),
                                               CreDtTm = (string)u.Element("CreDtTm")
                                           }).FirstOrDefault();

                    if (responseSummary != null)
                    {
                        orgMsgId = responseSummary.OrgMsgId;
                        creDtTm = responseSummary.CreDtTm;
                    }

                    var responseDetails = (from u in xDoc.Descendants("ResponseDetails")
                                           select new
                                           {
                                               PaymentRef = (string)u.Element("PaymentRef"),
                                               RespStatus = (string)u.Element("RespStatus"),
                                               Description = (string)u.Element("Description")
                                           }).FirstOrDefault();

                    if (responseDetails != null)
                    {
                        paymentRef = responseDetails.PaymentRef;
                        respStatus = responseDetails.RespStatus;
                        desc = responseDetails.Description;
                    }

                    if (headerVM.PaymentType == "P108")
                    {
                        var paymentFile = db.PaymentFiles.Where(a => a.MsgId == responseSummary.OrgMsgId).FirstOrDefault();
                        if (paymentFile != null)
                        {
                            paymentFile.OverallStatus = respStatus;
                            paymentFile.OverallStatusDesc = desc;

                            var paymentSummary = db.PaymentSummaries.Where(a => a.PaymentFileId == paymentFile.PaymentFileId).FirstOrDefault();
                            if (paymentSummary != null)
                            {
                                paymentSummary.OverallStatus = respStatus;
                                paymentSummary.OverallStatusDesc = desc;

                                var paymentVouchers = db.PaymentVouchers
                                    .Where(a => a.PaymentSummaryId == paymentSummary.PaymentSummaryId)
                                    .Where(a => a.OverallStatus != "SETTLED")
                                    .Where(a => a.OverallStatus != "UNAPPLIED")
                                    .ToList();
                                foreach (var item in paymentVouchers)
                                {
                                    item.OverallStatus = respStatus;
                                    item.OverallStatusDesc = desc;
                                }
                            }
                        }
                    }
                    else if (headerVM.PaymentType == "P120")
                    {
                        var paymentBatch = db.PaymentBatches.Where(a => a.MsgID == responseSummary.OrgMsgId).FirstOrDefault();

                        if (paymentBatch != null)
                        {
                            paymentBatch.OverallStatus = respStatus;
                            paymentBatch.OverallStatusDescription = desc;

                            var bulkPayments = db.BulkPayments
                                .Where(a => a.PaymentBatchID == paymentBatch.PaymentBatchID)
                                .Where(a => a.OverallStatus != "SETTLED")
                                .Where(a => a.OverallStatus != "UNAPPLIED")
                                .ToList();

                            foreach (var item in bulkPayments)
                            {
                                item.OverallStatus = respStatus;
                                item.SettledDescription = desc;
                                item.SettledAt = DateTime.Now;
                            }
                        }
                    }
                }

                db.SaveChanges();
            }
        }
        public ActionResult SendCancelMessage(string receiver, string orgMsgId, string paymentRef, string certStorePath, string certPass)
        {
            var xDoc = new XDocument(
                new XDeclaration("1.0", "utf-16", "yes"),
                            new XElement("Document",
                               new XElement("Header",
                                  new XElement("Sender", "MOFPTZTZ"),
                                  new XElement("Receiver", receiver),
                                  new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
                                  new XElement("PaymentType", "P108"),
                                  new XElement("MessageType", "CANCELLATION")),
                               new XElement("CancelDetails",
                                  new XElement("OrgMsgId", orgMsgId),
                                  new XElement("PaymentRef", paymentRef),
                                  new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                                  new XElement("Reason", "Wrong Payment"))));

            //Sign data

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            StringWriter sw = new StringWriter();
            using (XmlWriter xw = XmlWriter.Create(sw, settings))
            // or to write to a file...
            //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
            {
                xDoc.Save(xw);
            }

            var hashSignature = DigitalSignature.GenerateSignature(sw.ToString(), certStorePath, certPass);
            var signedData = sw.ToString() + "|" + hashSignature;

            return View();
        }

        public string GetResponse(BankingHeaderVM headerVM, string respStatus, string statusDesc)
        {
            var xmlResponse = new XDocument(
   new XDeclaration("1.0", "utf-16", "yes"),
               new XElement("Document",
                  new XElement("Header",
                     new XElement("Sender", headerVM.Receiver),
                     new XElement("Receiver", headerVM.Sender),
                     new XElement("MsgId", "MUSP" + DateTime.Now.ToString("yyyyMMddHHmmss")),
                     new XElement("PaymentType", headerVM.PaymentType),
                     new XElement("MessageType", "RESPONSE")),
                  new XElement("ResponseSummary",
                     new XElement("OrgMsgId", headerVM.MsgId),
                     new XElement("CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                  new XElement("ResponseDetails",
                     new XElement("PaymentRef", "NA"),
                     new XElement("RespStatus", respStatus),
                     new XElement("Description", statusDesc)))));

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            StringWriter sw = new StringWriter();
            using (XmlWriter xw = XmlWriter.Create(sw, settings))
            // or to write to a file...
            //using (XmlWriter xw = XmlWriter.Create(filePath, settings))
            {
                xmlResponse.Save(xw);
            }

            //check schema
            var schemaPath = Properties.Settings.Default.schemaFilePath + "schema_block_response.xsd";
            var validationResult = XMLTools.ValidateXml(xmlResponse, schemaPath);
            if (validationResult.HasErrors)
            {
                Log.Information(xmlResponse + "{Name}!", "OutgoingResponseInvalidSchemas");
                Log.Information(validationResult.ValidationDesc + "{Name}!", "OutgoingResponseInvalidSchemaDesc");
            }

            //Sign data
            var mofpCertStorePath = Properties.Settings.Default.MofpPrivatePfxPasswd;
            var mofpCertPass = Properties.Settings.Default.MofpPrivatePfxPath;
            var hashSignature = DigitalSignature.GenerateSignature(sw.ToString(), mofpCertStorePath, mofpCertPass);
            string response = sw.ToString() + "|" + hashSignature;
            Log.Information(response + "{Name}!", "GetResponseOutgoingResponses");

            return response;
        }
    }
}
