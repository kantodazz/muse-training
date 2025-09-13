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
using System.Configuration;

namespace IFMIS.Libraries
{
    public class ReconcilliationService
    {
        public static ReconcilliationResponse GetOutStandingGl(IFMISTZDbContext db, string BankAccount, DateTime CheckDate)
        {

            ReconcilliationResponse ReconcilliationResponse = new ReconcilliationResponse();
            ReconcilliationResponse.overallStatus = "Pending";

            string procedureName = "dbo.sp_GetOustandingRec";
            var connString = ConfigurationManager.ConnectionStrings["IFMISTZDbContext"].ConnectionString;
            SqlConnection sqlConn = new SqlConnection(connString);
            SqlCommand command = new SqlCommand(procedureName, sqlConn)
            {
                CommandTimeout = 1200,
                CommandType = CommandType.StoredProcedure
            };

            SqlParameter institutionParam = new SqlParameter("@BankAccount", BankAccount);
            institutionParam.Direction = ParameterDirection.Input;
            institutionParam.DbType = DbType.String;
            command.Parameters.Add(institutionParam);
            SqlParameter sbcParam = new SqlParameter("@CheckDate", CheckDate);
            sbcParam.Direction = ParameterDirection.Input;
            sbcParam.DbType = DbType.String;
            command.Parameters.Add(sbcParam);

            try
            {
                sqlConn.Open();

                DbDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    ReconcilliationResponse.GLOutoStandingVwList = new List<GLOutoStandingVw>();

                    while (reader.Read())
                    {
                        GLOutoStandingVw gLOutoStandingVw = new GLOutoStandingVw
                        {
                            ID = (long)reader["ID"],
                            BankAccount = reader["BankAccount"].ToString(),
                            AccountName = reader["AccountName"].ToString(),
                            GeneralLadgeID = (int)reader["GeneralLadgeID"],
                            LegalNumber = reader["LegalNumber"].ToString(),
                            DocumentNo = reader["DocumentNo"].ToString(),
                            BankAccountGL = reader["BankAccountGL"].ToString(),
                            DescriptionGL = reader["DescriptionGL"].ToString(),
                            TransactionDate = (DateTime)reader["TransactionDate"],
                            CheckDate = (DateTime)reader["CheckDate"],
                            TransactionType = reader["TransactionType"].ToString(),
                            ReconciliationStatus = reader["ReconciliationStatus"].ToString(),
                            OperationalAmountGL = (decimal)reader["OperationalAmountGL"],
                            BaseAmountGL = (decimal)reader["OperationalAmountGL"],
                            Age = (int)reader["Age"],

                        };

                        ReconcilliationResponse.GLOutoStandingVwList.Add(gLOutoStandingVw);
                    }
                }
                reader.Dispose();
            }
            catch (Exception ex)
            {
                ReconcilliationResponse.overallStatus = "Error";
                ReconcilliationResponse.overallStatusDescription = "Error Getting GL Outostanding " + ex.Message.ToString();
            }
            finally
            {
                sqlConn.Close();
            }
            return ReconcilliationResponse;
        }


        public static ReconcilliationResponse GetBankOutStanding(IFMISTZDbContext db, string BankAccount, DateTime CheckDate)
        {
            ReconcilliationResponse ReconcilliationResponse = new ReconcilliationResponse();
            ReconcilliationResponse.overallStatus = "Pending";

            string procedureName = "dbo.GetBankOutStanding_sp";
            var connString = ConfigurationManager.ConnectionStrings["IFMISTZDbContext"].ConnectionString;
            SqlConnection sqlConn = new SqlConnection(connString);
            SqlCommand command = new SqlCommand(procedureName, sqlConn)
            {
                CommandTimeout = 1200,
                CommandType = CommandType.StoredProcedure
            };

            SqlParameter institutionParam = new SqlParameter("@BankAccount", BankAccount);
            institutionParam.Direction = ParameterDirection.Input;
            institutionParam.DbType = DbType.String;
            command.Parameters.Add(institutionParam);
            SqlParameter sbcParam = new SqlParameter("@CheckDate", CheckDate);
            sbcParam.Direction = ParameterDirection.Input;
            sbcParam.DbType = DbType.String;
            command.Parameters.Add(sbcParam);

            try
            {
                sqlConn.Open();

                DbDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    ReconcilliationResponse.BankOutStandingVwList = new List<BankOutoStandingVw>();

                    while (reader.Read())
                    {
                        BankOutoStandingVw BankOutStandingVw = new BankOutoStandingVw
                        {
                            ID = (long)reader["ID"],
                            BankAccount = reader["BankAccount"].ToString(),
                            AccountName = reader["AccountName"].ToString(),
                            BankStatementID = (int)reader["BankStatementID"],
                            BankReference = reader["BankReference"].ToString(),
                            RelatedReference = reader["RelatedReference"].ToString(),
                            TransactionDescription = reader["TransactionDescription"].ToString(),
                            StatementDate = (DateTime)reader["StatementDate"],
                            CheckDate = (DateTime)reader["CheckDate"],
                            TransactionType = reader["TransactionType"].ToString(),
                            ReconciliationStatus = reader["ReconciliationStatus"].ToString(),
                            Amount = (decimal)reader["Amount"],
                            Age = (int)reader["Age"],
                        };
                        ReconcilliationResponse.BankOutStandingVwList.Add(BankOutStandingVw);
                    }
                }
                reader.Dispose();
            }
            catch (Exception ex)
            {
                ReconcilliationResponse.overallStatus = "Error";
                ReconcilliationResponse.overallStatusDescription = "Error Getting GL Outostanding " + ex.Message.ToString();
            }
            finally
            {
                sqlConn.Close();
            }
            return ReconcilliationResponse; ///sasa
        }
    }
}