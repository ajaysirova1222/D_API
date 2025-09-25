using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace D_API.services
{
    public class customerService
    {
        dbServices ds = new dbServices();
        public async Task<responseData> GetCustomerSubscriptions(requestData reqData)
        {
            responseData resData = new responseData();
            resData.eventID = reqData.eventID;

            try
            {
                if (!reqData.addInfo.ContainsKey("CustomerID") || string.IsNullOrEmpty(reqData.addInfo["CustomerID"].ToString()))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "CustomerID is mandatory.";
                    resData.rStatus = 400;
                    return resData;
                }

                string baseQuery = @"
                    SELECT CustomerID, CustomerName, SubscriptionName, SubscriptionCount, 
                           StartDate, EndDate, Active, CreatedDate, ModifiedDate
                    FROM customer_subscriptions 
                    WHERE CustomerID = @CustomerID";

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@CustomerID", reqData.addInfo["CustomerID"].ToString())
                };

                if (reqData.addInfo.ContainsKey("SubscriptionName") && !string.IsNullOrEmpty(reqData.addInfo["SubscriptionName"].ToString()))
                {
                    baseQuery += " AND SubscriptionName LIKE @SubscriptionName";
                    parameters.Add(new MySqlParameter("@SubscriptionName", $"%{reqData.addInfo["SubscriptionName"]}%"));
                }

                if (reqData.addInfo.ContainsKey("StartDate") && DateTime.TryParse(reqData.addInfo["StartDate"].ToString(), out DateTime startDate))
                {
                    baseQuery += " AND StartDate >= @StartDate";
                    parameters.Add(new MySqlParameter("@StartDate", startDate));
                }

                if (reqData.addInfo.ContainsKey("EndDate") && DateTime.TryParse(reqData.addInfo["EndDate"].ToString(), out DateTime endDate))
                {
                    baseQuery += " AND EndDate <= @EndDate";
                    parameters.Add(new MySqlParameter("@EndDate", endDate));
                }

                baseQuery += " ORDER BY CreatedDate DESC";

                var result = ds.ExecuteSQLName(baseQuery, parameters.ToArray());

                if (result[0].Count() == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "No subscriptions found for the given criteria.";
                    resData.rStatus = 404;
                }
                else
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "Subscriptions retrieved successfully.";
                    resData.rData["subscriptions"] = result[0];
                    resData.rData["totalCount"] = result[0].Count();
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = $"Error retrieving subscriptions: {ex.Message}";
                resData.rStatus = 500;
            }

            return resData;
        }

        public async Task<responseData> AddCustomerSubscription(requestData reqData)
        {
            responseData resData = new responseData();
            resData.eventID = reqData.eventID;

            try
            {
                string[] requiredFields = { "CustomerID", "CustomerName", "SubscriptionName", "StartDate", "EndDate" };
                foreach (string field in requiredFields)
                {
                    if (!reqData.addInfo.ContainsKey(field) || string.IsNullOrEmpty(reqData.addInfo[field].ToString()))
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = $"{field} is required.";
                        resData.rStatus = 400;
                        return resData;
                    }
                }

                if (!DateTime.TryParse(reqData.addInfo["StartDate"].ToString(), out DateTime startDate) ||
                    !DateTime.TryParse(reqData.addInfo["EndDate"].ToString(), out DateTime endDate))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Invalid date format. Use YYYY-MM-DD.";
                    resData.rStatus = 400;
                    return resData;
                }

                if (startDate >= endDate)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Start date must be before end date.";
                    resData.rStatus = 400;
                    return resData;
                }

                string checkQuery = @"
                    SELECT COUNT(*) as count 
                    FROM customer_subscriptions 
                    WHERE CustomerID = @CustomerID AND SubscriptionName = @SubscriptionName 
                    AND Active = 1 AND EndDate > NOW()";

                MySqlParameter[] checkParams = new MySqlParameter[]
                {
                    new MySqlParameter("@CustomerID", reqData.addInfo["CustomerID"].ToString()),
                    new MySqlParameter("@SubscriptionName", reqData.addInfo["SubscriptionName"].ToString())
                };

                var checkResult = ds.ExecuteSQLName(checkQuery, checkParams);
                if (checkResult[0].Count() > 0 || checkResult != null)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Active subscription with the same name already exists for this customer.";
                    resData.rStatus = 409;
                    return resData;
                }

                string insertQuery = @"
                    INSERT INTO customer_subscriptions 
                    (CustomerID, CustomerName, SubscriptionName, SubscriptionCount, StartDate, EndDate, Active, CreatedDate) 
                    VALUES (@CustomerID, @CustomerName, @SubscriptionName, @SubscriptionCount, @StartDate, @EndDate, @Active, NOW());";

                MySqlParameter[] parameters = new MySqlParameter[]
                {
                    new MySqlParameter("@CustomerID", reqData.addInfo["CustomerID"].ToString()),
                    new MySqlParameter("@CustomerName", reqData.addInfo["CustomerName"].ToString()),
                    new MySqlParameter("@SubscriptionName", reqData.addInfo["SubscriptionName"].ToString()),
                    new MySqlParameter("@SubscriptionCount",reqData.addInfo["SubscriptionCount"].ToString()),
                    new MySqlParameter("@StartDate", startDate),
                    new MySqlParameter("@EndDate", endDate),
                    new MySqlParameter("@Active", reqData.addInfo["Active"].ToString()),
                };

                int result = ds.ExecuteInsertAndGetLastId(insertQuery, parameters);

                if (result > 0)
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "Subscription added successfully.";
                    resData.rData["subscriptionID"] = result;
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Failed to add subscription.";
                    resData.rStatus = 500;
                }
            }
            catch (Exception ex) 
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = $"Error adding subscription: {ex.Message}";
                resData.rStatus = 500;
            }

            return resData;
        }

        public async Task<responseData> UpdateCustomerSubscription(requestData reqData)
        {
            responseData resData = new responseData();
            resData.eventID = reqData.eventID;

            try
            {
                if (!reqData.addInfo.ContainsKey("ID") || string.IsNullOrEmpty(reqData.addInfo["ID"].ToString()))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Subscription ID is required for update.";
                    resData.rStatus = 400;
                    return resData;
                }

                if (!DateTime.TryParse(reqData.addInfo["StartDate"].ToString(), out DateTime startDate) ||
                    !DateTime.TryParse(reqData.addInfo["EndDate"].ToString(), out DateTime endDate))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Invalid date format. Use YYYY-MM-DD.";
                    resData.rStatus = 400;
                    return resData;
                }

                if (startDate >= endDate)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Start date must be before end date.";
                    resData.rStatus = 400;
                    return resData;
                }

                string checkQuery = "SELECT COUNT(*) as count FROM customer_subscriptions WHERE ID = @ID";
                MySqlParameter[] checkParams = new MySqlParameter[]
                {
                    new MySqlParameter("@ID", reqData.addInfo["ID"])
                };

                var checkResult = ds.ExecuteSQLName(checkQuery, checkParams);
                if (checkResult[0].Count() == 0 || checkResult == null)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Subscription not found.";
                    resData.rStatus = 404;
                    return resData;
                }

                MySqlParameter[] parameters = new MySqlParameter[]
                 {
                    new MySqlParameter("@CustomerID", reqData.addInfo["CustomerID"].ToString()),
                    new MySqlParameter("@CustomerName", reqData.addInfo["CustomerName"].ToString()),
                    new MySqlParameter("@SubscriptionName", reqData.addInfo["SubscriptionName"].ToString()),
                    new MySqlParameter("@SubscriptionCount",reqData.addInfo["SubscriptionCount"].ToString()),
                    new MySqlParameter("@StartDate", startDate),
                    new MySqlParameter("@EndDate", endDate),
                    new MySqlParameter("@Active", reqData.addInfo["Active"].ToString()),
                    new MySqlParameter("@ModifiedDate", DateTime.Now)
                 };

                string updateQuery = @"UPDATE customer_subscriptions SET CustomerID = @CustomerID, CustomerName = @CustomerName, 
                SubscriptionName = @SubscriptionName, SubscriptionCount = @SubscriptionCount, StartDate = @StartDate, EndDate = @EndDate,
                 Active = @Active, ModifiedDate = @ModifiedDate
                 WHERE ID = @ID";

                var result = ds.executeSQL(updateQuery, parameters);

                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Subscription updated successfully.";
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = $"Error updating subscription: {ex.Message}";
                resData.rStatus = 500;
            }

            return resData;
        }
        public async Task<responseData> DeleteCustomerSubscription(requestData reqData)
        {
            responseData resData = new responseData();
            resData.eventID = reqData.eventID;

            try
            {
                if (!reqData.addInfo.ContainsKey("ID") || string.IsNullOrEmpty(reqData.addInfo["ID"].ToString()))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Subscription ID is required for deletion.";
                    resData.rStatus = 400;
                    return resData;
                }

                // Check if subscription exists
                string checkQuery = "SELECT COUNT(*) as count FROM customer_subscriptions WHERE ID = @ID";
                MySqlParameter[] checkParams = new MySqlParameter[]
                {
                    new MySqlParameter("@ID", reqData.addInfo["ID"])
                };

                var checkResult = ds.ExecuteSQLName(checkQuery, checkParams);
                if (checkResult[0].Count() == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Subscription not found.";
                    resData.rStatus = 404;
                    return resData;
                }

                // Soft delete - just set Active to false
                string deleteQuery = @"
                    UPDATE customer_subscriptions 
                    SET Active = 0, ModifiedDate = NOW()
                    WHERE ID = @ID";

                MySqlParameter[] parameters = new MySqlParameter[]
                {
                    new MySqlParameter("@ID", reqData.addInfo["ID"])
                };

                var result = ds.executeSQL(deleteQuery, parameters);

                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Subscription deleted successfully.";
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = $"Error deleting subscription: {ex.Message}";
                resData.rStatus = 500;
            }

            return resData;
        }

        public async Task<responseData> DeleteCustomerSubscription1(requestData reqData)   // Permanent delete
        {
            responseData resData = new responseData();

            try
            {
                if (!reqData.addInfo.ContainsKey("ID") || string.IsNullOrEmpty(reqData.addInfo["ID"].ToString()))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Subscription ID is required for deletion.";
                    resData.rStatus = 400;
                    return resData;
                }

                // Check if subscription exists
                string checkQuery = "SELECT COUNT(*) as count FROM customer_subscriptions WHERE ID = @ID";
                MySqlParameter[] checkParams = new MySqlParameter[]
                {
                    new MySqlParameter("@ID", reqData.addInfo["ID"])
                };

                var checkResult = ds.ExecuteSQLName(checkQuery, checkParams);
                if (checkResult[0].Count() == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Subscription not found.";
                    resData.rStatus = 404;
                    return resData;
                }

                string deleteQuery = @"DELETE FROM customer_subscriptions WHERE ID = @ID;";
                MySqlParameter[] deleteParams = new MySqlParameter[]
                {
            new MySqlParameter("@ID", reqData.addInfo["ID"].ToString())
                };

                var result = ds.executeSQL(deleteQuery, deleteParams);

                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Subscription deleted successfully.";
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Error: " + ex.Message;
            }

            return resData;
        }
         public async Task<responseData> UpdateSubscriptionCount(requestData reqData)
        {
            responseData resData = new responseData();
            resData.eventID = reqData.eventID;

            try
            {
                if (!reqData.addInfo.ContainsKey("CustomerID") || string.IsNullOrEmpty(reqData.addInfo["CustomerID"].ToString()) ||
                    !reqData.addInfo.ContainsKey("SubscriptionName") || string.IsNullOrEmpty(reqData.addInfo["SubscriptionName"].ToString()) ||
                    !reqData.addInfo.ContainsKey("CountChange") || !int.TryParse(reqData.addInfo["CountChange"].ToString(), out int countChange))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "CustomerID, SubscriptionName, and CountChange (integer) are required.";
                    resData.rStatus = 400;
                    return resData;
                }

                // Find active subscription
                string findQuery = @"
                    SELECT ID, SubscriptionCount 
                    FROM customer_subscriptions 
                    WHERE CustomerID = @CustomerID AND SubscriptionName = @SubscriptionName AND Active = 1 
                    ORDER BY CreatedDate DESC LIMIT 1";

                MySqlParameter[] findParams = new MySqlParameter[]
                {
                    new MySqlParameter("@CustomerID", reqData.addInfo["CustomerID"].ToString()),
                    new MySqlParameter("@SubscriptionName", reqData.addInfo["SubscriptionName"].ToString())
                };

                var findResult = ds.ExecuteSQLName(findQuery, findParams);

                if (findResult[0].Count() == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Active subscription not found for the given criteria.";
                    resData.rStatus = 404;
                    return resData;
                }

                int currentCount = Convert.ToInt32(findResult[0][0]["SubscriptionCount"]);
                int newCount = currentCount + countChange;

                // Prevent negative counts
                if (newCount < 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = $"Cannot reduce count below zero. Current count: {currentCount}, Requested change: {countChange}";
                    resData.rStatus = 400;
                    return resData;
                }

                string updateQuery = @"
                    UPDATE customer_subscriptions 
                    SET SubscriptionCount = @NewCount, ModifiedDate = NOW()
                    WHERE ID = @ID";

                MySqlParameter[] updateParams = new MySqlParameter[]
                {
                    new MySqlParameter("@NewCount", newCount),
                    new MySqlParameter("@ID", Convert.ToInt32(findResult[0][0]["ID"]))
                };

                var result = ds.executeSQL(updateQuery, updateParams);

                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Subscription count updated successfully.";
                resData.rData["previousCount"] = currentCount;
                resData.rData["newCount"] = newCount;
                resData.rData["change"] = countChange;
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = $"Error updating subscription count: {ex.Message}";
                resData.rStatus = 500;
            }

            return resData;
        }

    }
}