using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Community.CsharpSqlite;
using FILE = System.IO.TextWriter;
using GETPROCTIMES = System.IntPtr;
using HANDLE = System.IntPtr;
using HINSTANCE = System.IntPtr;
using sqlite3_int64 = System.Int64;
using u32 = System.UInt32;
using va_list = System.Object;
using clean = CleanSqlite;

namespace DatIndexParser
{
    using dxCallback = Sqlite3.dxCallback;
    using FILETIME = Sqlite3.FILETIME;
    using sqlite3 = Sqlite3.sqlite3;
    using sqlite3_stmt = Sqlite3.Vdbe;
    using sqlite3_value = Sqlite3.Mem;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;

    public class Database
    {
        class previous_mode_data
        {
            public bool valid;        /* Is there legit data in here? */
            public int mode;
            public bool showHeader;
            public int[] colWidth = new int[200];
        };
        /*
       ** An pointer to an instance of this structure is passed from
       ** the main program to the callback.  This is used to communicate
       ** state and mode information.
       */
        class callback_data
        {
            public Sqlite3.sqlite3 db;            /* The database */
            public bool echoOn;           /* True to echo input commands */
            public bool statsOn;          /* True to display memory stats before each finalize */
            public int cnt;               /* Number of records displayed so far */
            public FILE Out;             /* Write results here */
            public int mode;              /* An output mode setting */
            public bool writableSchema;  /* True if PRAGMA writable_schema=ON */
            public bool showHeader;      /* True to show column names in List or Column mode */
            public string zDestTable;     /* Name of destination table when MODE_Insert */
            public string separator = ""; /* Separator character for MODE_List */
            public int[] colWidth = new int[200];      /* Requested width of each column when in column mode*/
            public int[] actualWidth = new int[200];   /* Actual width of each column */
            public string nullvalue = "NULL";          /* The text to print when a null comes back from
** the database */
            public previous_mode_data explainPrev = new previous_mode_data();
            /* Holds the mode information just before
            ** .explain ON */
            public StringBuilder outfile = new StringBuilder(260); /* Filename for Out */
            public string zDbFilename;    /* name of the database file */
            public string zVfs;           /* Name of VFS to use */
            public sqlite3_stmt pStmt;   /* Current statement if any. */
            public FILE pLog;            /* Write log output here */

            internal callback_data Copy()
            {
                return (callback_data)this.MemberwiseClone();
            }
        };
        // Store callback data variant 
        class callback_data_extra
        {
            public string[] azCols; //(string *)pData;      /* Names of result columns */
            public string[] azVals;//azCols[nCol];         /* Results */
            public int[] aiTypes;   //(int *)&azVals[nCol]; /* Result types */
        }
        const int MODE_Line = 0;
        const int MODE_Column = 1;
        const int MODE_List = 2;
        const int MODE_Semi = 3;
        const int MODE_Html = 4;
        const int MODE_Insert = 5;
        const int MODE_Tcl = 6;
        const int MODE_Csv = 7;
        const int MODE_Explain = 8;

        callback_data data = null;
        static Sqlite3.sqlite3 dbToQuery;
        static bool go =true;
        public static int NumberOfThreads = 0;
        delegate void ExecSqlDelegate(string statement);
        static Sqlite3.sqlite3 pDb;
        static readonly object Locker = new object();
        List<string> queryQueue = new List<string>();
        int rc;
        string dbToMergeWith;
        string insertStatement;
        static List<string> tables =  new List<string>();
        static byte[] encKey;
        public static bool createNew = false;

        /// <summary>
        /// Initialize with db connection
        /// </summary>
        /// <param name="outputDb"></param>
        public Database(string outputDb)
        {
            Debug.Print(String.Format("Section {0} Thread {1}", "Database initialized", Thread.CurrentThread.ManagedThreadId));
            if (pDb == null)
            {
                rc = 0;
                dbToMergeWith = outputDb;
                NumberOfThreads = 0;
            }
            else
            {
                NumberOfThreads++;
            }
        }
        ~Database()
        {
            
        }

        static void UNUSED_PARAMETER<T>(T x) { }
        static int shell_callback(object pArg, sqlite3_int64 nArg, object p2, object p3)
        {
            int i;
            callback_data p = (callback_data)pArg;

            //Unpack
            string[] azArg = ((callback_data_extra)p2).azVals;
            string[] azCol = ((callback_data_extra)p2).azCols;
            int[] aiType = ((callback_data_extra)p2).aiTypes;

            if (p.cnt++ == null && p.showHeader)
            {
                for (i = 0; i < nArg; i++)
                {
                    //fprintf(p.Out, "%s%s", azCol[i], i == nArg - 1 ? "\n" : p.separator);
                }
            }
            if (azArg == null)
                return 0;
            for (i = 0; i < nArg; i++)
            {
                string z = azArg[i];
                if (z == null)
                    z = p.nullvalue;
                //fprintf(p.Out, "%s", z);
                if (i < nArg - 1)
                {
                   // fprintf(p.Out, "%s", p.separator);
                }
                else
                {
                    //fprintf(p.Out, "\n");
                }
            }
            return 0;
        }
        /*
       ** Execute a statement or set of statements.  Print 
       ** any result rows/columns depending on the current mode 
       ** set via the supplied callback.
       **
       ** This is very similar to SQLite's built-in Sqlite3.sqlite3_exec() 
       ** function except it takes a slightly different callback 
       ** and callback data argument.
       */
        static int shell_exec(
        sqlite3 db,                       /* An open database */
        string zSql,                      /* SQL to be evaluated */
        dxCallback xCallback,             //int (*xCallback)(void*,int,char**,char**,int*),   /* Callback function */
            /* (not the same as Sqlite3.sqlite3_exec) */
        callback_data pArg,               /* Pointer to struct callback_data */
        ref string pzErrMsg               /* Error msg written here */
        )
        {
            sqlite3_stmt pStmt = null;        /* Statement to execute. */
            int rc = Sqlite3.SQLITE_OK;       /* Return Code */
            string zLeftover = null;          /* Tail of unprocessed SQL */

            //  if( pzErrMsg )
            {
                pzErrMsg = null;
            }

            while (!String.IsNullOrEmpty(zSql) && (Sqlite3.SQLITE_OK == rc))
            {
                rc = Sqlite3.sqlite3_prepare_v2(db, zSql, -1, ref pStmt, ref zLeftover);
                if (Sqlite3.SQLITE_OK != rc)
                {
                    //if (pzErrMsg != null)
                    {
                        //pzErrMsg = save_err_msg(db);
                    }
                }
                else
                {
                    if (null == pStmt)
                    {
                        /* this happens for a comment or white-space */
                        zSql = zLeftover.TrimStart();
                        //while (isspace(zSql[0]))
                        //    zSql++;
                        continue;
                    }

                    /* save off the prepared statment handle and reset row count */
                    if (pArg != null)
                    {
                        pArg.pStmt = pStmt;
                        pArg.cnt = 0;
                    }

                    /* echo the sql statement if echo on */
                    if (pArg != null && pArg.echoOn)
                    {
                        string zStmtSql = Sqlite3.sqlite3_sql(pStmt);
                        //fprintf(pArg.Out, "%s\n", zStmtSql != null ? zStmtSql : zSql);
                    }

                    /* perform the first step.  this will tell us if we
                    ** have a result set or not and how wide it is.
                    */
                    rc = Sqlite3.sqlite3_step(pStmt);
                    /* if we have a result set... */
                    if (Sqlite3.SQLITE_ROW == rc)
                    {
                        /* if we have a callback... */
                        if (xCallback != null)
                        {
                            /* allocate space for col name ptr, value ptr, and type */
                            int nCol = Sqlite3.sqlite3_column_count(pStmt);
                            //void pData = Sqlite3.SQLITE_malloc(3*nCol*sizeof(const char*) + 1);
                            //if( !pData ){
                            //  rc = Sqlite3.SQLITE_NOMEM;
                            //}else
                            {
                                string[] azCols = new string[nCol];//(string *)pData;    /* Names of result columns */
                                string[] azVals = new string[nCol];//azCols[nCol];       /* Results */
                                int[] aiTypes = new int[nCol];//(int *)&azVals[nCol]; /* Result types */
                                int i;
                                //Debug.Assert(sizeof(int) <= sizeof(string )); 
                                /* save off ptrs to column names */
                                for (i = 0; i < nCol; i++)
                                {
                                    azCols[i] = (string)Sqlite3.sqlite3_column_name(pStmt, i);
                                }
                                do
                                {
                                    /* extract the data and data types */
                                    for (i = 0; i < nCol; i++)
                                    {
                                        azVals[i] = (string)Sqlite3.sqlite3_column_text(pStmt, i);
                                        aiTypes[i] = Sqlite3.sqlite3_column_type(pStmt, i);
                                        if (null == azVals[i] && (aiTypes[i] != Sqlite3.SQLITE_NULL))
                                        {
                                            rc = Sqlite3.SQLITE_NOMEM;
                                            break; /* from for */
                                        }
                                    } /* end for */

                                    /* if data and types extracted successfully... */
                                    if (Sqlite3.SQLITE_ROW == rc)
                                    {
                                        /* call the supplied callback with the result row data */
                                        callback_data_extra cde = new callback_data_extra();
                                        cde.azVals = azVals;
                                        cde.azCols = azCols;
                                        cde.aiTypes = aiTypes;

                                        if (xCallback(pArg, nCol, cde, null) != 0)
                                        {
                                            rc = Sqlite3.SQLITE_ABORT;
                                        }
                                        else
                                        {
                                            rc = Sqlite3.sqlite3_step(pStmt);
                                        }
                                    }
                                } while (Sqlite3.SQLITE_ROW == rc);
                                //Sqlite3.sqlite3_free(ref pData);
                            }
                        }
                        else
                        {
                            do
                            {
                                rc = Sqlite3.sqlite3_step(pStmt);
                            } while (rc == Sqlite3.SQLITE_ROW);
                        }
                    }

                    

                    /* Finalize the statement just executed. If this fails, save a 
                    ** copy of the error message. Otherwise, set zSql to point to the
                    ** next statement to execute. */
                    rc = Sqlite3.sqlite3_finalize(pStmt);
                    if (rc == Sqlite3.SQLITE_OK)
                    {
                        zSql = zLeftover.TrimStart();
                        //while (isspace(zSql[0]))
                        //    zSql++;
                    }
                    

                    /* clear saved stmt handle */
                    if (pArg != null)
                    {
                        pArg.pStmt = null;
                    }
                }
            } /* end while */

            return rc;
        }
        /*
        ** Make sure the database is open.  If it is not, then open it.  If
        ** the database fails to open, print an error message and exit.
        */
        static void open_db(callback_data p)
        {
            if (p.db == null)
            {
                Sqlite3.sqlite3_open(p.zDbFilename, out p.db);
                dbToQuery = p.db;
                if (dbToQuery != null && Sqlite3.sqlite3_errcode(dbToQuery) == Sqlite3.SQLITE_OK)
                {
                    Sqlite3.sqlite3_create_function(dbToQuery, "shellstatic", 0, Sqlite3.SQLITE_UTF8, 0,
                    shellstaticFunc, null, null);
                }
                if (dbToQuery == null || Sqlite3.SQLITE_OK != Sqlite3.sqlite3_errcode(dbToQuery))
                {
                    //fprintf(stderr, "Error: unable to open database \"%s\": %s\n",
                    //p.zDbFilename, Sqlite3.sqlite3_errmsg(db));
                    //exit(1);
                }
            }
        }
        /*
       ** A callback for the Sqlite3.SQLITE_log() interface.
       */
        static void shellLog(object pArg, int iErrCode, string zMsg)
        {
            callback_data p = (callback_data)pArg;
            if (p.pLog == null)
                return;
            Debug.Print("(%d) %s\n", iErrCode, zMsg);
            //fflush(p.pLog);
        }
        /*
        ** A global char* and an SQL function to access its current value 
        ** from within an SQL statement. This program used to use the 
        ** Sqlite3.sqlite3_exec_printf() API to substitue a string into an SQL statement.
        ** The correct way to do this with sqlite3 is to use the bind API, but
        ** since the shell is built around the callback paradigm it would be a lot
        ** of work. Instead just use this hack, which is quite harmless.
        */
        static string zShellStatic = "";
        static void shellstaticFunc(
        Sqlite3.sqlite3_context context,
        int argc,
        sqlite3_value[] argv
        )
        {
            Debug.Assert(0 == argc);
            Debug.Assert(String.IsNullOrEmpty(zShellStatic));
            UNUSED_PARAMETER(argc);
            UNUSED_PARAMETER(argv);
            Sqlite3.sqlite3_result_text(context, zShellStatic, -1, Sqlite3.SQLITE_STATIC);
        }

        /*
        ** Initialize the state information in data
        */
        public void main_init()
        {
            
            data = new callback_data();//memset(data, 0, sizeof(*data));
            //data.mode = MODE_List;
            data.separator = "|";//memcpy(data.separator, "|", 2);
            data.showHeader = false;
            Sqlite3.sqlite3_initialize();
            Sqlite3.sqlite3_config(Sqlite3.SQLITE_CONFIG_URI, 1);
            Sqlite3.sqlite3_config(Sqlite3.SQLITE_CONFIG_LOG, new object[] { (Sqlite3.dxLog)shellLog, data, null });
            Sqlite3.sqlite3_config(Sqlite3.SQLITE_CONFIG_SERIALIZED);
        }
        
        public void MemoryMerge()
        {
            Monitor.Enter(Locker);

            sqlite3 backup = new sqlite3();
            rc = Sqlite3.sqlite3_open(dbToMergeWith, out backup);
            Sqlite3.sqlite3_backup pBack = Sqlite3.sqlite3_backup_init(backup, "main", pDb, "main");
            rc = Sqlite3.sqlite3_errcode(backup);
            rc = Sqlite3.sqlite3_errcode(pDb);
            rc = Sqlite3.sqlite3_backup_pagecount(pBack);
            do
            {
                rc = Sqlite3.sqlite3_backup_step(pBack, 50);
                if (rc == Sqlite3.SQLITE_OK || rc == Sqlite3.SQLITE_BUSY || rc == Sqlite3.SQLITE_LOCKED)
                {
                    Sqlite3.sqlite3_sleep(25);
                }
            } while (rc == Sqlite3.SQLITE_OK || rc == Sqlite3.SQLITE_BUSY || rc == Sqlite3.SQLITE_LOCKED);

            rc = Sqlite3.sqlite3_errcode(backup);
            rc = Sqlite3.sqlite3_errcode(pDb);
            Sqlite3.sqlite3_backup_finish(pBack);
            rc = Sqlite3.sqlite3_errcode(backup);
            rc = Sqlite3.sqlite3_errcode(pDb);
            if (rc == 0 && Sqlite3.ms.Length > 0)
            {
                MemoryStream ms = Sqlite3.ms;
                long howMuchToCompress = ms.Length;
                byte[] buffer = new byte[howMuchToCompress];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                if (createNew)
                {
                    using (FileStream fs = new FileStream("history.db", FileMode.Create))
                    {
                        fs.Write(buffer, 0, buffer.Length);
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream("history.db", FileMode.OpenOrCreate))
                    {
                        fs.Write(buffer, 0, buffer.Length);
                    }
                }
                Sqlite3.sqlite3_close(pDb);
                return;

            }
            Sqlite3.sqlite3_close(pDb);
        }
        /// <summary>
        /// Creates a table, string format should be "field type primary key, nextfield type"
        /// </summary>
        /// <returns></returns>
        public void Create(string table, string[] cols)
        {
            string strQueryFront = "CREATE TABLE " + table + " (";
            insertStatement = "INSERT INTO " + table + " (";
            for (int i = 0; i < cols.Length; i++)
            {
                if (i < cols.Length - 1)
                {
                    strQueryFront += cols[i] + ", ";
                    cols[i] = cols[i].Replace(" TEXT", "");
                    cols[i] = cols[i].Replace(" INTEGER PRIMARY KEY ASC", "");
                    cols[i] = cols[i].Replace(" INTEGER PRIMARY KEY", "");
                    cols[i] = cols[i].Replace(" INTEGER", "");
                    cols[i] = cols[i].Replace(" DATETIME", "");
                    insertStatement += cols[i] + ", ";
                }
                else
                {
                    strQueryFront += cols[i] + ");";
                    cols[i] = cols[i].Replace(" TEXT", "");
                    cols[i] = cols[i].Replace(" INTEGER PRIMARY KEY ASC", "");
                    cols[i] = cols[i].Replace(" INTEGER PRIMARY KEY", "");
                    cols[i] = cols[i].Replace(" INTEGER", "");
                    cols[i] = cols[i].Replace(" DATETIME", "");
                    insertStatement += cols[i] + ") VALUES (";
                }
            }
            ExecSql(strQueryFront);
        }

        public int Insert(string table, string[] values)
        {
            string strQueryFront = insertStatement;
            string strQueryEnd = "";
            for (int i = 0; i < values.Length;i++ )
            {
                if (values[i] == "NULL")
                {
                    if (i < values.Length - 1)
                    {
                        strQueryEnd += "NULL,";
                    }
                    else
                    {
                        strQueryEnd += "NULL);";
                    }
                }
                else
                {
                    if (i < values.Length - 1)
                    {
                        strQueryEnd += "'" + values[i] + "', ";
                    }
                    else
                    {
                        strQueryEnd += "'" + values[i] + "');";
                    }
                }
            }
            strQueryFront += strQueryEnd;
            return ExecSql(strQueryFront);
            
        }
        int ExecSql(string statement)
        {

            Sqlite3.sqlite3_exec(pDb, statement, 0, 0, 0);
            rc = Sqlite3.sqlite3_errcode(pDb);
            return rc;

        }
        /// <summary>
        /// Test which history file we are dealing with
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>0 for FF 1 for chrome 2 for error</returns>
        public int WhichHistoryFileType(string filename)
        {
            //ff is between 11 and 20, chrome is 1-10
            string firefox = "SELECT COUNT() FROM moz_places;";
            string firefoxDownloads = "SELECT COUNT() FROM moz_downloads;";
            string chrome = "SELECT COUNT() FROM urls;";
            string chromeArchived = "SELECT COUNT() FROM pages_content;";
            string chromeCookie = "SELECT COUNT() FROM cookies;";
            clean.Sqlite3.sqlite3 pFFdb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pFFdb);
            if (rc != 0)
            {
                return 0;
            }
            rc = clean.Sqlite3.exec(pFFdb, firefox, 0, 0, 0);
            string errmsg = clean.Sqlite3.sqlite3_errmsg(pFFdb);
            if (rc == 0)
            {
                clean.Sqlite3.sqlite3_close(pFFdb);
                return 11;
            }
            rc = clean.Sqlite3.exec(pFFdb, firefoxDownloads, 0, 0, 0);
            if (rc == 0)
            {
                clean.Sqlite3.sqlite3_close(pFFdb);
                return 12;
            }
            rc = clean.Sqlite3.exec(pFFdb, chrome, 0, 0, 0);
            if(rc == 0)
            {
                clean.Sqlite3.sqlite3_close(pFFdb);
                return 1;
            }
            rc = clean.Sqlite3.exec(pFFdb, chromeArchived, 0, 0, 0);
            if (rc == 0)
            {
                clean.Sqlite3.sqlite3_close(pFFdb);
                return 2;
            }
            rc = clean.Sqlite3.exec(pFFdb, chromeCookie, 0, 0, 0);
            if (rc == 0)
            {
                clean.Sqlite3.sqlite3_close(pFFdb);
                return 3;
            }
            else
            {
                clean.Sqlite3.sqlite3_close(pFFdb);
                return 0;
            }
        }
        public void AttachFFHistoryFile(string filename,string[] colNames, out DatIndexParser.Program.FFHistoryEntry results)
        {
            string create = "SELECT datetime(" + colNames[0] + "/1000000,'unixepoch'), " + colNames[1] + "," + colNames[2] + ",datetime(" + colNames[3] + "/1000000,'unixepoch'), "+colNames[4]+", "+colNames[5]+" FROM moz_places, moz_historyvisits WHERE moz_places.id = moz_historyvisits.place_id;";
            results.results = null;
            results.colNames = null;
            string[] strRows = null;
            string err = null;
            int rows = 0;
            int cols = 0;
            results.colNames = new string[colNames.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                results.colNames[i] = colNames[i];
            }
            clean.Sqlite3.sqlite3 pFFdb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pFFdb);
            //rc = clean.Sqlite3.sqlite3_exec(pFFdb, create, 0, 0, 0);
            rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref strRows, ref rows, ref cols, ref err);
            results.results = strRows;
            //rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref results, ref rows, ref cols, ref err);
            clean.Sqlite3.sqlite3_close(pFFdb);
            /*
            string attach = "ATTACH '" + filename + "' AS firefox_db;";
            ExecSql(attach);
            //string create = "CREATE TABLE " + tablename + " AS " + statement;
            
            //rc = shell_exec(pDb,create,shell_callback,data,ref attach);
            //ExecSql(create);
            string detach = "DETACH firefox_db;";
            ExecSql(detach);
            */
        }
        public void AttachFFHistoryFileEmbedded(string filename, string[] colNames, out DatIndexParser.Program.FFHistoryEntry results)
        {
            string create = "SELECT " + colNames[0] + ", " + colNames[1] + "," + colNames[2] + " FROM moz_places WHERE moz_places.hidden != '0'";
            results.results = null;
            results.colNames = null;
            string[] strRows = null;
            string err = null;
            int rows = 0;
            int cols = 0;
            results.colNames = new string[colNames.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                results.colNames[i] = colNames[i];
            }
            clean.Sqlite3.sqlite3 pFFdb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pFFdb);
            //rc = clean.Sqlite3.sqlite3_exec(pFFdb, create, 0, 0, 0);
            rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref strRows, ref rows, ref cols, ref err);
            results.results = strRows;
            //rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref results, ref rows, ref cols, ref err);
            clean.Sqlite3.sqlite3_close(pFFdb);
            /*
            string attach = "ATTACH '" + filename + "' AS firefox_db;";
            ExecSql(attach);
            //string create = "CREATE TABLE " + tablename + " AS " + statement;
            
            //rc = shell_exec(pDb,create,shell_callback,data,ref attach);
            //ExecSql(create);
            string detach = "DETACH firefox_db;";
            ExecSql(detach);
            */
        }
        public void AttachFFHistoryFileDownloads(string filename, string[] colNames, out DatIndexParser.Program.FFHistoryEntry results)
        {
            //"moz_downloads.source", "moz_downloads.startTime", "moz_downloads.endTime", "moz_downloads.name", "moz_downloads.currBytes", "moz_downloads.maxBytes"
            string create = "SELECT " + colNames[0] + ", datetime(" + colNames[1] + "/1000000,'unixepoch'), datetime(" + colNames[2] + "/1000000,'unixepoch'), " +
                colNames[3] + ", " + colNames[4] + ", " + colNames[5] + " FROM moz_downloads;";
            results.results = null;
            results.colNames = null;
            string[] strRows = null;
            string err = null;
            int rows = 0;
            int cols = 0;
            results.colNames = new string[colNames.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                results.colNames[i] = colNames[i];
            }
            clean.Sqlite3.sqlite3 pFFdb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pFFdb);
            //rc = clean.Sqlite3.sqlite3_exec(pFFdb, create, 0, 0, 0);
            rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref strRows, ref rows, ref cols, ref err);
            results.results = strRows;
            //rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref results, ref rows, ref cols, ref err);
            clean.Sqlite3.sqlite3_close(pFFdb);
            /*
            string attach = "ATTACH '" + filename + "' AS firefox_db;";
            ExecSql(attach);
            //string create = "CREATE TABLE " + tablename + " AS " + statement;
            
            //rc = shell_exec(pDb,create,shell_callback,data,ref attach);
            //ExecSql(create);
            string detach = "DETACH firefox_db;";
            ExecSql(detach);
            */
        }
        public void AttachChromeHistoryFile(string filename, string[] colNames, out DatIndexParser.Program.ChromeHistoryEntry results)
        {
            string create = "SELECT datetime((" + colNames[0] + "- 11644473600000000 ) / 1000000 ,'unixepoch')," + colNames[1] + "," + colNames[2] + ",datetime((" + colNames[3] + "- 11644473600000000 ) / 1000000 ,'unixepoch') "+
                ",term,urls.title,urls.visit_count,urls.hidden FROM urls LEFT OUTER JOIN keyword_search_terms ON urls.id = keyword_search_terms.url_id, visits WHERE urls.id = visits.url;";
            results.results = null;
            results.colNames = null;
            string[] strRows = null;
            string err = null;
            int rows = 0;
            int cols = 0;
            results.colNames = new string[colNames.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                results.colNames[i] = colNames[i];
            }
            clean.Sqlite3.sqlite3 pChromedb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pChromedb);
            //rc = clean.Sqlite3.sqlite3_exec(pFFdb, create, 0, 0, 0);
            rc = clean.Sqlite3.sqlite3_get_table(pChromedb, create, ref strRows, ref rows, ref cols, ref err);
            results.results = strRows;
            //rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref results, ref rows, ref cols, ref err);
            clean.Sqlite3.sqlite3_close(pChromedb);
            /*
            string attach = "ATTACH '" + filename + "' AS firefox_db;";
            ExecSql(attach);
            //string create = "CREATE TABLE " + tablename + " AS " + statement;
            
            //rc = shell_exec(pDb,create,shell_callback,data,ref attach);
            //ExecSql(create);
            string detach = "DETACH firefox_db;";
            ExecSql(detach);
            */
        }
        public void AttachChromeHistoryFileArchive(string filename, string[] colNames, out DatIndexParser.Program.ChromeHistoryEntry results)
        {
            string create = "SELECT datetime((" + colNames[0] + "- 11644473600000000 ) / 1000000 ,'unixepoch')," + colNames[1] + ", " + colNames[2] + " " +
                "FROM pages_content, info WHERE info.rowid = pages_content.rowid;";
            //SELECT datetime(( info.time - 11644473600000000 ) / 1000000 ,'unixepoch'),pages_content.c0url FROM pages_content, info WHERE info.rowid = pages_content.rowid;
            results.results = null;
            results.colNames = null;
            string[] strRows = null;
            string err = null;
            int rows = 0;
            int cols = 0;
            results.colNames = new string[colNames.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                results.colNames[i] = colNames[i];
            }
            clean.Sqlite3.sqlite3 pChromedb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pChromedb);
            //rc = clean.Sqlite3.sqlite3_exec(pFFdb, create, 0, 0, 0);
            rc = clean.Sqlite3.sqlite3_get_table(pChromedb, create, ref strRows, ref rows, ref cols, ref err);
            results.results = strRows;
            //rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref results, ref rows, ref cols, ref err);
            clean.Sqlite3.sqlite3_close(pChromedb);
            /*
            string attach = "ATTACH '" + filename + "' AS firefox_db;";
            ExecSql(attach);
            //string create = "CREATE TABLE " + tablename + " AS " + statement;
            
            //rc = shell_exec(pDb,create,shell_callback,data,ref attach);
            //ExecSql(create);
            string detach = "DETACH firefox_db;";
            ExecSql(detach);
            */
        }
        public void AttachChromeHistoryFileCookies(string filename, string[] colNames, out DatIndexParser.Program.ChromeHistoryEntry results)
        {
            string create = "SELECT " + colNames[0] + ", datetime((" + colNames[1] + "- 11644473600000000 ) / 1000000 ,'unixepoch'), datetime((" + colNames[2] + " - 11644473600000000 ) / 1000000 ,'unixepoch')," + colNames[3] + 
                " FROM cookies;";
            //SELECT datetime(( info.time - 11644473600000000 ) / 1000000 ,'unixepoch'),pages_content.c0url FROM pages_content, info WHERE info.rowid = pages_content.rowid;
            results.results = null;
            results.colNames = null;
            string[] strRows = null;
            string err = null;
            int rows = 0;
            int cols = 0;
            results.colNames = new string[colNames.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                results.colNames[i] = colNames[i];
            }
            clean.Sqlite3.sqlite3 pChromedb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pChromedb);
            //rc = clean.Sqlite3.sqlite3_exec(pFFdb, create, 0, 0, 0);
            rc = clean.Sqlite3.sqlite3_get_table(pChromedb, create, ref strRows, ref rows, ref cols, ref err);
            results.results = strRows;
            //rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref results, ref rows, ref cols, ref err);
            clean.Sqlite3.sqlite3_close(pChromedb);
            /*
            string attach = "ATTACH '" + filename + "' AS firefox_db;";
            ExecSql(attach);
            //string create = "CREATE TABLE " + tablename + " AS " + statement;
            
            //rc = shell_exec(pDb,create,shell_callback,data,ref attach);
            //ExecSql(create);
            string detach = "DETACH firefox_db;";
            ExecSql(detach);
            */
        }
        public void AttachChromeHistoryFileDownloads(string filename, string[] colNames, out DatIndexParser.Program.ChromeHistoryEntry results)
        {
            string create = "SELECT datetime(" + colNames[0] + ",'unixepoch'), datetime(" + colNames[1] + " ,'unixepoch')," + colNames[2] + ", " + colNames[3] +
                ", " + colNames[4] + ", " + colNames[5] + " FROM downloads;";
            //SELECT datetime(( downloads.start_time - 11644473600000000 ) / 1000000 ,'unixepoch'),datetime(( downloads.end_time - 11644473600000000 ) / 1000000 ,'unixepoch'),
                                //downloads.url,downloads.full_path,downloads.received_bytes,downloads.total_bytes FROM downloads;
            results.results = null;
            results.colNames = null;
            string[] strRows = null;
            string err = null;
            int rows = 0;
            int cols = 0;
            results.colNames = new string[colNames.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                results.colNames[i] = colNames[i];
            }
            clean.Sqlite3.sqlite3 pChromedb = null;
            rc = clean.Sqlite3.sqlite3_open(filename, out pChromedb);
            //rc = clean.Sqlite3.sqlite3_exec(pFFdb, create, 0, 0, 0);
            rc = clean.Sqlite3.sqlite3_get_table(pChromedb, create, ref strRows, ref rows, ref cols, ref err);
            results.results = strRows;
            //rc = clean.Sqlite3.sqlite3_get_table(pFFdb, create, ref results, ref rows, ref cols, ref err);
            clean.Sqlite3.sqlite3_close(pChromedb);
            /*
            string attach = "ATTACH '" + filename + "' AS firefox_db;";
            ExecSql(attach);
            //string create = "CREATE TABLE " + tablename + " AS " + statement;
            
            //rc = shell_exec(pDb,create,shell_callback,data,ref attach);
            //ExecSql(create);
            string detach = "DETACH firefox_db;";
            ExecSql(detach);
            */
        }
        public void CreateMemory()
        {
            try
            {
                if (pDb == null)
                {
                    Sqlite3.ms = new MemoryStream();
                    Sqlite3.sqlite3_open(":memory:", out pDb);
                    rc = Sqlite3.sqlite3_errcode(pDb);
                    data.db = pDb;
                    if (rc == 0)
                    {
                        Sqlite3.sqlite3_create_function(pDb, "shellstatic", 0, Sqlite3.SQLITE_UTF8, 0, shellstaticFunc, null, null);
                        rc = Sqlite3.sqlite3_errcode(pDb);
                        Sqlite3.sqlite3_enable_load_extension(pDb, 1);
                        rc = Sqlite3.sqlite3_errcode(pDb);
                        Debug.Print(String.Format("Section {0} Thread {1} created a db", "CreateMemory", Thread.CurrentThread.ManagedThreadId));
                    }
                    else
                    {
                        Debug.Print(String.Format("Section {0} Thread {1} failed to create a db", "CreateMemory", Thread.CurrentThread.ManagedThreadId));
                    }
                }
                else
                {
                    Debug.Print(String.Format("Section {0} Thread {1} failed to create a db", "CreateMemory", Thread.CurrentThread.ManagedThreadId));
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(String.Format("Section {0} Thread {1} failed to create a db", "CreateMemory", Thread.CurrentThread.ManagedThreadId));
                rc = Sqlite3.sqlite3_errcode(pDb);
            }
        }
        void DumpQueue()
        {
            foreach (string s in queryQueue)
            {
                Sqlite3.sqlite3_exec(pDb, s, 0, 0, 0);
            }
        }
        public int GetLastDBError()
        {
            return rc;
        }
    }
}
