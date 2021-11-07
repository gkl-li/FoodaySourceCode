using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient;

public class DBManager : MonoBehaviour
{
    public static MySqlConnection mySqlConnection;
    static string host = "103.108.228.62";
    static string user = "fooday-usr";
    static string pwd = "JDMShdthpmf2E4fx";
    static string db = "fooday-db";

    private static void connectDB()
    {
        string connectionString = string.Format("Server={0};Database={1};Uid={2};Pwd={3};CharSet=utf8;", host, db, user, pwd);
        mySqlConnection = new MySqlConnection(connectionString);
        mySqlConnection.Open();
        print("Opened");
    }


    public static void UpdateDB(string query)
    {
        connectDB();

        //string query = "UPDATE User SET JSON='Changed'";
        MySqlCommand cmd = new MySqlCommand();
        //Assign the query using CommandText
        cmd.CommandText = query;
        //Assign the connection using Connection
        cmd.Connection = mySqlConnection;

        //Execute query
        cmd.ExecuteNonQuery();

        //close connection
        mySqlConnection.Close();
    }

    public static List<string> Select(string attr,string table,string conditions)
    {
        connectDB();

        string query = string.Format("SELECT {0} FROM {1} WHERE {2}", attr, table, conditions.Length == 0 ? " 1;" : conditions);
        //Create a list to store the result
        List<string> list = new List<string>();

        //Open connection
        if (mySqlConnection.State == System.Data.ConnectionState.Open)
        {
            //Create Command
            MySqlCommand cmd = new MySqlCommand(query, mySqlConnection);
            //Create a data reader and Execute the command
            MySqlDataReader dataReader = cmd.ExecuteReader();

            //Read the data and store them in the list
            while (dataReader.Read())
            {
                list.Add(dataReader[attr] + "");
            }

            //close Data Reader
            dataReader.Close();

            //close Connection
            mySqlConnection.Close();

            //return list to be displayed
            return list;
        }
        else
        {
            return list;
        }
    }

    public void Close()
    {
        if (mySqlConnection != null)
        {
            mySqlConnection.Close();
            mySqlConnection.Dispose();
            mySqlConnection = null;
        }
    }
}