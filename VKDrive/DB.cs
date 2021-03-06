﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive
{
    class Db : IDisposable
    {
        public SQLiteConnection SqLite;
        private sealed class SingletonCreator
        {
            private static readonly Db instance = new Db();
            public static Db Instance { get { return instance; } }
        }

        public static Db Instance
        {
            get { 
                return SingletonCreator.Instance; 
            }
        }

        public void Connect()
        {
            if (SqLite != null)
            {
                SqLite.Close();
            }
            SqLite = new SQLiteConnection("Data Source=:memory:;New=True;");
            SqLite.Open();


            Execute(@"CREATE TABLE file_parts( 
                uid INTEGER NOT NULL, 
                fid INTEGER NOT NULL, 
                block_id INTEGER NOT NULL,
                byte INTEGER NOT NULL,
                byte_offset INTEGER NOT NULL,
                status TINYINT NOT NULL, 
                    PRIMARY KEY ( uid, fid, block_id))");
            Execute(@"INSERT OR IGNORE INTO file_parts 
                        ( uid, fid, block_id, byte, byte_offset, status ) VALUES 
                        ( 0,    0,     0,      0,      0,          2)");


        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public SQLiteDataReader Execute(string textCommand)
        {
            SQLiteCommand command = new SQLiteCommand(textCommand, SqLite);
            //Console.WriteLine(textCommand);
            //command.CommandText = textCommand;
            //command.CommandType = CommandType.Text;
            
            return command.ExecuteReader();
        }

        public void Dispose()
        {
            if (SqLite != null)
            {
                SqLite.Close();
            }
        }
    }
}
