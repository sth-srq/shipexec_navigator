using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using System.Data.SqlClient;
using PSI.Sox.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace PSI.Sox
{
    public class DataService
    {
        public ILogger Logger { get; set; }

        public enum DataSetName
        {
            HEADER,
            COMMODITY,
            PACKAGES,
            INSERT
        }
        public enum BatchFileType
        {
            DELIMITED,
            FIXWIDTH
        }
        public DataService(ILogger logger)
        {
            Logger = logger;
        }

        public DataSet GetDataByKeyNumber(string connectionString, string keyNumber, DataSetName dataSetName)
        {
            DataSet dataset = new DataSet();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand(GetSQLQuery(dataSetName), connection))
                    {
                        sqlCommand.CommandType = CommandType.Text;
                        sqlCommand.CommandText = GetSQLQuery(dataSetName);
                        sqlCommand.Parameters.Add("@datakey", SqlDbType.VarChar).Value = keyNumber;

                        using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                        {
                            dataAdapter.SelectCommand = sqlCommand;
                            Logger.Log(this, LogLevel.Info, "Attempting to open connection in GetDataByKeyNumber()");
                            connection.Open();
                            Logger.Log(this, LogLevel.Info, "Opened connection in GetDataByKeyNumber()");
                            dataAdapter.Fill(dataset);
                        }
                    }
                }

                if (dataset.Tables.Count > 0 && dataset.Tables[0].Rows.Count == 0)
                {
                    Logger.Log(this, LogLevel.Error, "No data was found for " + keyNumber + " in GetDataByKeyNumber()");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Error, "Error searching for '" + keyNumber + "' GetDataByKeyNumber [" + dataSetName + "]" + ex.Message);
                throw new Exception("Error searching for '" + keyNumber + "' GetDataByKeyNumber [" + dataSetName + "]" + ex.Message);
            }

            return dataset;
        }

        private string GetSQLQuery(DataSetName dataSetName)
        {
            StringBuilder query = new StringBuilder();
            switch (dataSetName)
            {
                case DataSetName.HEADER:
                    query.Append("SELECT * ");
                    query.Append("FROM [DemoCustomerDb].[dbo].[OpenOrderDb] ");
                    query.Append("WHERE order_id = @datakey");
                    break;
                case DataSetName.COMMODITY:
                    query.Append("SELECT * ");
                    query.Append("FROM [DemoCustomerDb].[dbo].[OrderDetailLinesDb] ");
                    query.Append("WHERE order_id = @datakey");
                    break;
                case DataSetName.PACKAGES:
                    break;
                case DataSetName.INSERT:
                    break;
                default:
                    throw new Exception("Invalid DataSetName");
            }
            Logger.Log(this, LogLevel.Trace, "SQL select to get consignee info in GetSQLQuery() " + query.ToString());
            return query.ToString();
        }

        public void InsertShippedData(string connectionString, string keyNumber, ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse, DataSetName dataSetName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand(GetSQLQuery(dataSetName), connection))
                    {
                        sqlCommand.CommandType = CommandType.Text;
                        sqlCommand.CommandText = GetSQLQuery(dataSetName);
                        sqlCommand.Parameters.AddWithValue("@ordernum", shipmentRequest.Packages[0].ShipperReference);
                        sqlCommand.Parameters.AddWithValue("@freightcost", shipmentRequest.Packages[0].ShipperReference);
                        sqlCommand.Parameters.AddWithValue("@shippedatetime", DateTime.Now);
                        connection.Open();
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Error, "Error inserting data for '" + keyNumber + "' InsertShippedDate [" + dataSetName + "]" + ex.Message);
                throw new Exception("Error inserting data for '" + keyNumber + "' InsertShippedDate [" + dataSetName + "]" + ex.Message);
            }
        }

        public DataSet ParseBatchFile(Stream fileStream, BatchFileType batchFileType)
        {
            try
            {
                DataSet dataSet = new DataSet();
                DataTable dataTable = new DataTable();
                dataTable.TableName = "batchfiledata";

                if (dataTable.Columns.Count == 0)
                {
                    List<string> columnsNames = getColumnNames();
                    foreach (var name in columnsNames)
                    {
                        dataTable.Columns.Add(name);
                    }
                }

                using (TextFieldParser parser = new TextFieldParser(fileStream))
                {
                    if (batchFileType == BatchFileType.DELIMITED)
                    {
                        parser.Delimiters = new string[] { "," };
                        parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    }

                    if (batchFileType == BatchFileType.FIXWIDTH)
                    {
                        parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.FixedWidth;
                        parser.SetFieldWidths(4, 6, 35, 35, 35, 35, 24, 14, 5, 4, 10, 4, 5, 15, 15, 15, 28, 5, 5, 5);
                    }

                    bool fileContainsHeader = true;
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        if (fileContainsHeader)
                        {
                            fileContainsHeader = false;
                            continue;
                        }
                        dataTable.Rows.Add(fields);
                    }
                }

                if (dataTable.Rows.Count == 0)
                {
                    dataTable = null;
                    Logger.Log(this, LogLevel.Info, "No data was found for for File Batching in ParseBatchFile()");
                }

                dataSet.Tables.Add(dataTable);
                return dataSet;
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Info, "ERROR... Could not create Batch file data in ParseBatchFile()... " + ex.Message);
                throw new Exception("ERROR... Could not ParseBatchFile to be used for batching..." + ex.Message);
            }
        }

        private List<string> getColumnNames()
        {
            List<string> columnName = new List<string>
            {
                "PackageSeq","ShipFromReturnCompany","ShipFromReturnContact","ShipFromReturnAddress1","ShipFromReturnAddress2","ShipFromReturnAddress3","ShipFromReturnCity","ShipFromReturnState","ShipFromReturnZipcode","ShipFromReturnCountry","ShipFromReturnPhone","ShipToCompany","ShipToContact","ShipToAddress1","ShipToAddress2","ShipToAddress3","ShipToCity","ShipToState","ShipToZipcode","ShipToCountry","ShipToPhone","FriendlyService","Service","Packaging","PackageWeight","Length","Width","Height","ShipperReference","ConsigneeReference","EmailAddress","ReturnLabelNeeded","ProofRequireSignature","SaturdayDelivery","DocumentsOnly","Description","Item1ProductCode","Item1Description","Item1OriginCountry","Item1Quantity","Item1UnitValue","Item1UnitWeight","Item2ProductCode","Item2Description","Item2OriginCountry","Item2Quantity","Item2UnitValue","Item2UnitWeight","Item3ProductCode","Item3Description","Item3OriginCountry","Item3Quantity","Item3UnitValue","Item3UnitWeight","Item4ProductCode","Item4Description","Item4OriginCountry","Item4Quantity","Item4UnitValue","Item4UnitWeight","Item5ProductCode","Item5Description","Item5OriginCountry","Item5Quantity","Item5UnitValue","Item5UnitWeight"
            };
            return columnName;
        }
    }
}
