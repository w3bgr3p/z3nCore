
using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore
{
    
    public partial class Db
    {
        private readonly IZennoPosterProjectModel _project;
        public Db(
            IZennoPosterProjectModel project,
            string dbMode = null,
            string sqLitePath = null,
            string pgHost = null,
            string pgPort = null,
            string pgDbName = null,
            string pgUser = null,
            string pgPass = null,
            string defaultTable = null)
        {
            _project = project;
            _dbMode = dbMode ?? project.Var("DBmode");
            _sqLitePath = sqLitePath ?? project.Var("DBsqltPath");
            _pgHost = pgHost ?? project.GVar("sqlPgHost");
            _pgPort = pgPort ?? project.GVar("sqlPgPort");
            _pgDbName = pgDbName ?? project.GVar("sqlPgName");
            _pgUser = pgUser ?? project.GVar("sqlPgUser");
            _pgPass = pgPass ?? project.Var("DBpstgrPass");
            _defaultTable = defaultTable ?? project.ProjectTable();
        }
    }
}