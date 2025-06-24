using Dlubal.RSTAB8;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
namespace Verrollungsnachweis
{
    //public delegate void StateChangeEventDelegate(object sender, _Event _event);
    public static class _Counter
    {
        public static IEnumerable<(T item, int index)> Indexel<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
    public enum Elem
    {
        point,
        points,
        linie,
        linies
    }
    public class MyForce
    {
        private double _n;
        private double _t;
        private double _tpern;
        private bool nSet = false, tSet = false;
        public double G { get; set; }
        public double N
        {
            get { return _n; }
            set
            {
                _n = value;
                nSet = true;
                //if (tSet) Update_tpern();
            }
        }
        public double T
        {
            get { return _t; }
            set
            {
                _t = value;
                tSet = true;
                //if (nSet) Update_tpern();
            }
        }
        //public double TperN => _tpern;
        public MyForce() { }
        /*void Update_tpern()
        {
            if (Math.Abs(T) > 10e-6 && N > 10e-6) _tpern = _t / _n;
            else if (Math.Abs(T) < 10e-6) _tpern = 0;
            else if (N <0) _tpern = 1;
            else if (N < 10e-6) _tpern = 2 * _t;
        }*/
    }
    public class MyLoadcase
    {
        public int Index { get; set; }
        public int QIndex { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string LoadType { get; set; }
        public List<MyForce> Force { get; set; } = new List<MyForce>();
        public override string ToString()
        {
            return $"{Number} \t {Name}";
        }
        public MyLoadcase(MyLoadcase other)
        {
            this.Index = other.Index;
            this.QIndex = other.QIndex;
            this.Number = other.Number;
            this.Name = other.Name;
            this.Force = other.Force;
            this.LoadType = other.LoadType;
        }
        public MyLoadcase(int index, string number, string name)
        {
            Index = index;
            QIndex = -1; // Default value for QIndex
            Number = number;
            Name = name;
        }
    }
    class HelperFunc
    {
        private static readonly Lazy<HelperFunc> _instance =
         new Lazy<HelperFunc>(() => new HelperFunc());
        private HelperFunc() { } 
        public static HelperFunc Instance => _instance.Value;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ConnectionManager connectionManager = new ConnectionManager();
        IApplication app = null;
        IModel model = null;
        public List<int> selectElement = null;
        public List<int> selected = null;
        public ResultCombination resultComb;
        public string loadCaseByLanguage = "LC";
        public string _case;
        public string modelName = "";
        List<int> angledRod = new List<int>();
        public List<double> deg = new List<double>();
        public double TDurchmesser;
        public MyForce[] GGforce;
        public List<MyLoadcase> loadcaseList = new List<MyLoadcase>();
        public List<MyLoadcase> g_lastenList = new List<MyLoadcase>();
        public List<MyLoadcase> q_lastenList = new List<MyLoadcase>();
        public List<MyLoadcase> gq_lastenList = new List<MyLoadcase>();
        public MyLoadcase Gegengewicht;
        public List<MyForce> totalG = new List<MyForce>();
        public List<MyForce> totalG_with_GG = new List<MyForce>();
        public Dictionary<Node, Member[]> supportParts = new Dictionary<Node, Member[]>();
        List<int> verticalSupport = new List<int>();
        public List<int> tangentialSupport = new List<int>();
        public List<double[]> normal_Forces = new List<double[]>();
        public List<double[]> tangential_Forces = new List<double[]>();
        public List<double[]> vertikal_Forces = new List<double[]>();
        public List<int[]> Verrollung_TKs = new List<int[]>();
        public List<int[]> RowNumberInExcel = new List<int[]>();
        public double[] MaxTangentialkraft = new double[2];
        List<string[]> all_Q_LFNo_inGroups = new List<string[]>();
        string[] allLFNo_in_LC;
        public int[] supportRotateDirection = new int[2] { 0, 0 };
        private int? gegengewichtIndex;
        double[] Ergebnis_trad_sum = new double[2];
        double[] Bestratio_new2 = new double[2];
        public int? GegengewichtIndex
        {
            get { return gegengewichtIndex; }
            set
            {
                if (value.HasValue)
                {
                    gegengewichtIndex = value;
                    Gegengewicht = gq_lastenList[(int)gegengewichtIndex];
                }
            }
        }


        public void GetConnect()
        {
            try
            {
                (app, model) = connectionManager.GetConnect();
                connectionManager.GetConnect();
                modelName = model.GetName();
            }
            catch (RstabConnectionException ex)
            {
                MessageBox.Show(ex.Message + ex.ErrorType, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                logger.Error(ex.Message + ":  " + ex.ErrorType + "  " + ex.InnerException?.StackTrace);
                ConnectionManager.Kill_Background_Process("RSTAB8");
                System.Windows.Forms.Application.Restart();
                TheEnd();
            }

        }
        public bool IsConnected()
        {
            try
            {
                return connectionManager.IsConnected();

            }
            catch (RstabConnectionException ex)
            {
                if (ex.InnerException is RstabConnectionException rc && rc.ErrorType == ConnectionErrorType.ModelNotActive)
                {

                    DialogResult result = MessageBox.Show($"Keine Verbindung zum Model:{modelName}.rs8\n Hast du ihn zugemacht?\n Wenn 'JA', ich muss auch zu. Wenn 'NEIN', setze das Modell in das aktive Fenster!", "Kérdés", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        return false;
                    }
                }

                MessageBox.Show(ex.Message + ex.ErrorType, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                logger.Error(ex.Message + ":  " + ex.ErrorType + "  " + ex.InnerException?.StackTrace);
                ConnectionManager.Kill_Background_Process("RSTAB8");
                System.Windows.Forms.Application.Restart();
                TheEnd();

            }
            return false;

        }

        public List<string> GetEKName()
        {
            List<string> EKName = new List<string>();
            ILoads loads = null;
            ResultCombination[] loadings = null;
            try
            {

                connectionManager.LockAndUnlockLicense(() =>
                {
                    model = app.GetActiveModel();
                    loads = model.GetLoads();
                    loadings = loads.GetResultCombinations();
                });
                foreach (var (item, index) in loadings.Indexel())
                {
                    if (index == 0)
                    {
                        loadCaseByLanguage = (item.Definition.Substring(0, 2));
                    }
                    int r = loadings.ToList().IndexOf(item);
                    EKName.Add(item.Description);
                }
                return EKName;
            }
            catch (Exception e)
            {
                
                logger.Error(e, "GetEKName() failed");
                return null;
            }
        }
        /// <summary>
        /// Returns the number of the selected Result Combination in the combobox
        /// </summary>
        /// <param name="combo"></param>
        /// <returns></returns>
        public int AtItemEkToAtNoEK(ComboBox combo)
        {
            ICalculation calculation;
            ILoads loads = null;
            try
            {
               
                if (!IsConnected()) { return -1; }
                ;
                connectionManager.LockAndUnlockLicense(() =>
                {
                    calculation = model.GetCalculation();
                    loads = model.GetLoads();
                });


                for (int i = combo.SelectedIndex; i < loads.GetLastObjectNo(LoadingType.ResultCombinationType); i++)
                // All result combinations (AtNo= also counts those places where no LoadCombination is specified
                //For example, 1,2 are specified, 3 is empty, 4 is specified then GetLastObjectNo=4
                {
                    try
                    {
                        string vmi = loads.GetResultCombination(i + 1, ItemAt.AtNo).GetData().Description;//Switch occurs from AtIndex to AtNo, hence i+1
                        if (vmi == combo.SelectedItem.ToString())
                        {
                            return i + 1;
                        }
                        ;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                TheEnd();
                return -1;
            }
           
            return -1;
        }
        /// <summary>
        ///Puts the number of Points or Members selected into the selected integer List
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public bool SelectElement(Elem elem)
        {
            try
            {
                string objects = "";
                if (!IsConnected()) { return false; }
                connectionManager.LockAndUnlockLicense(() =>
                {
                    //Dlubal.RSTAB8.IModel model = app.GetActiveModel();
                    IView view = model.GetActiveView();
                    
                    app.Show();
                    if ((elem == Elem.point) || (elem == Elem.points))
                    {
                        view.PickObjects(ToolType.SelectNodes, ref objects);
                    }
                    else
                    {
                        view.PickObjects(ToolType.SelectMembers, ref objects);
                    }
                    
                });
                if (objects == "")
                {
                    return false;
                }
                if ((objects.Contains(",") || objects.Contains("-")) && (elem == Elem.point || elem == Elem.linie))
                {
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult result;
                    string message = "1 Element ist erlaubt zu auswahlen";
                    string caption = "Fehler";
                    result = MessageBox.Show(message, caption, buttons);
                    return false;
                }
                selectElement = new List<int>(StringToIntList(objects));
                selected = new List<int>(StringToIntList(objects));
                return true;
            }
            catch (System.Exception)
            {
                TheEnd();
                return false;
            }
        }
        public List<int> StringToIntList(string textT)
        {
            try
            {
                List<int> Nr = new List<int>();
                while (textT.Contains("-") || textT.Contains(","))
                {
                    int a = textT.IndexOf(',');
                    int b = textT.IndexOf('-');
                    if (((a < b) & (a > 0)) || (b < 0))
                    {
                        Nr.Add(Convert.ToInt32(textT.Remove(a)));
                        textT = textT.Substring(a + 1);
                    }
                    else
                    {
                        int tol = Convert.ToInt32(textT.Remove(b));
                        if (textT.Contains(","))
                        {
                            int ig = Convert.ToInt32(textT.Substring((b + 1), ((a - b) - 1)));
                            for (int i = tol; i <= ig; i++)
                            {
                                Nr.Add(i);
                            }
                            textT = textT.Substring(a + 1);
                        }
                        else
                        {
                            int ig = Convert.ToInt32(textT.Substring(b + 1));
                            for (int i = tol; i < ig; i++)
                            {
                                Nr.Add(i);
                            }
                            textT = textT.Substring(b + 1);
                        }
                    }
                }
                if (textT != "")
                {
                    Nr.Add(Convert.ToInt32(textT));
                }
                else
                {
                    Nr.Add(0);
                }
                return Nr;
            }
            catch (Exception)
            {
                return null;
            }
        } 
        public bool VerrollFindSupport(Dictionary<Node, Member[]> connectingMembers)
        {
            try
            {
                if (!IsConnected()) { return false; }
                connectionManager.LockAndUnlockLicense(() =>
                {
                    //Dlubal.RSTAB8.IModel model = app.GetActiveModel();
                    IModelData data = model.GetModelData();
                    NodalSupport[] allSupports = data.GetNodalSupports();
                    Member[] members = data.GetMembers();
                    Node[] nodes = data.GetNodes();
                    model.SetDescription("Verrollungsnachweis");
                    model.SetComment("");
                    model.SetModified();
                    foreach (var item in connectingMembers)
                    {
                        verticalSupport.Add(item.Key.No);
                        angledRod.Add(item.Value[0].No);
                        angledRod.Add(item.Value[1].No);
                        foreach (var supportType in allSupports)
                        {
                            if (supportType.RotationAngles.X == 0 && supportType.RotationAngles.Y == 0 && supportType.RotationAngles.Z == 0)
                            {
                                continue;
                            }
                            foreach (var tamaszNr in StringToIntList(supportType.NodeList).ToArray())
                            {
                                if (tamaszNr == item.Value[0].StartNodeNo || tamaszNr == item.Value[0].EndNodeNo || tamaszNr == item.Value[1].StartNodeNo || tamaszNr == item.Value[1].EndNodeNo)
                                {
                                    tangentialSupport.Add(tamaszNr);
                                    break;
                                }
                            }
                        }
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        if (nodes[FindIndex(nodes, tangentialSupport[i])].Y < nodes[FindIndex(nodes, verticalSupport[i])].Y)
                        {
                            supportRotateDirection[i] = 1; //In the table, if the support is on the right side, the sign of the shear force remains
                        }
                        else
                        {
                            supportRotateDirection[i] = -1; //In the table, if the support is on the left side, the sign of the shear force changes
                        }
                    }
                });
                return true;
            }
            catch
            {
                TheEnd();
                return false;
            }
        }
        public int FindIndex(Member[] tomb, int Nr)
        {
            int index = Array.FindIndex(tomb, Member => Member.No == Nr);
            return index;
        }
        public int FindIndex(Node[] tomb, int Nr)
        {
            int index = Array.FindIndex(tomb, Node => Node.No == Nr);
            return index;
        }
        public int LCNoToIndex(string LCNo)
        {
            foreach (var item in loadcaseList)
            {
                if (LCNo == item.Number) return item.Index;
            }
            return 0;
        }
        ///  Collects the serial numbers of useful loads qlasten in the governing load combination into List (int[]) ExcellSorszam
        /// Based on the MAXIMUM SHEAR FORCE / Normal force METHOD. This is OK.
        public List<int[]> CalculatedExcelRow_Traditional()
        {
            List<int[]> ints = new List<int[]>();
           
            for (int i = 0; i < 2; i++)
            {
                double maxTangential = totalG_with_GG[i].T;
                double maxNormal = totalG_with_GG[i].N;
                ;

                if (Verrollung_TKs.Count != 2)
                {  
                    logger.Error("CalculatedExcelRow2(): Verrollung_TKs.Count != 2 ");
                    MessageBox.Show("Aufgrund der Struktur der Tabelle kann das Programm nur 4 Fahrwerk berücksichtigen. ");
                    TheEnd();
                    return null;
                }
                List<int> curr = new List<int>();

                foreach (var lf in loadcaseList)
                {
                    int.TryParse(lf.Number.Substring(2), out int num);
                    if (Verrollung_TKs[i].Contains( num) && lf.QIndex!=-1)
                    {
                        curr.Add(lf.QIndex);
                        maxTangential += lf.Force[i].T;
                        maxNormal += lf.Force[i].N;
                    }
                        
                }
                Ergebnis_trad_sum[i] = Math.Abs(maxTangential/Math.Abs(maxNormal));
                ints.Add(curr.ToArray());
            }

            
            
            return ints;
        }

        public static IEnumerable<List<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<List<T>> result = new[] { new List<T>() };
            foreach (var sequence in sequences)
            {
                result = from acc in result
                         from item in sequence
                         select new List<T>(acc) { item };
            }
            return result;
        }

        internal List<List<MyLoadcase>> groupedQ(List<string[]> all_Q_LFNo_inGroups)
        {

            return all_Q_LFNo_inGroups
             .Select(group =>
             loadcaseList
             .Where(lc => group.ToList().Contains(lc.Number))
             .Where(lc => lc.QIndex != -1)
             .ToList()
             )
             .ToList();

        }
        internal (List<MyLoadcase> BestCombo, double BestRatio) FindBestCombination(
         List<MyLoadcase> allCases, int i)
        {
            
            var mandatory = loadcaseList.Where(lc => lc.LoadType == "G").ToList();

            List<List<MyLoadcase>> groupedQLoads = new List<List<MyLoadcase>>();
            groupedQLoads=groupedQ(all_Q_LFNo_inGroups);
            
            var groupOptions = groupedQLoads
                .Select(group => new List<MyLoadcase> { new MyLoadcase(900,"","") }.Concat(group))
                .ToList();

            var bestCombo = new List<MyLoadcase>();
            double bestRatio = double.MinValue;

            foreach (var combo in CartesianProduct(groupOptions))
            {
                //var selected = combo.Where(x => x != null).Cast<MyLoadcase>().ToList();
                var selected = combo.Where(x => x != null && x.Index != 900).ToList();
                var fullCombo = new List<MyLoadcase>(mandatory);

                fullCombo.AddRange(selected);

                double sumN = fullCombo.Sum(lc => lc.Force[i].N);
                double sumT = fullCombo.Sum(lc => lc.Force[i].T);

                if (sumN != 0)
                {
                    double ratio = Math.Abs(sumT / sumN);
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestCombo = new List<MyLoadcase>(fullCombo);
                    }
                }
            }

            return (bestCombo, bestRatio);
        }

        public List<int[]> CalculatedExcelRow_New()
        {
            List<List<MyLoadcase>> BestCombo_new2 = new List<List<MyLoadcase>>();
            for (int i = 0; i < 2; i++)
            {
                var Ergebnis = FindBestCombination(loadcaseList, i);
                BestCombo_new2.Add(Ergebnis.BestCombo);
                Bestratio_new2[i] = (Ergebnis.BestRatio);
            }
            //List<int[]> Ergebnis_new2 = BestCombo_new2
            return BestCombo_new2
 .Select(combo => combo
 .Where(lc => lc.QIndex > -1)
 .Select(lc => lc.QIndex)
 .ToArray())
 .ToList();
        }
        public void CompareExcerRow()
        {
            
            List<int[]> Ergebnis_trad = new List<int[]>(CalculatedExcelRow_Traditional());
            List<int[]> Ergebnis_new = new List<int[]>(CalculatedExcelRow_New());

            bool areEqual = Ergebnis_trad.Count == Ergebnis_new.Count &&
            Ergebnis_trad.Zip(Ergebnis_new, (a, b) => a.SequenceEqual(b)).All(equal => equal);

            if (areEqual)
            {
                RowNumberInExcel = new List<int[]>(Ergebnis_new);
                return;
            }
            else
            {
                string[] Ergebnis_trad_str = new string[2] ;
                string[] Ergebnis_new_str = new string[2];
                

                for (int i = 0; i < 2; i++)
                {
                    string trad_str="";
                    string neu_str="";

                    foreach (var lf in q_lastenList)
                    { 
                        if (Ergebnis_trad[i].Contains(lf.QIndex))
                        {
                            trad_str = trad_str + "+"+ lf.Number ;
                        }
                        if (Ergebnis_new[i].Contains(lf.QIndex))
                        {
                            neu_str = neu_str + "+" + lf.Number;
                        }
                    }
                    Ergebnis_trad_str[i] = trad_str;
                    Ergebnis_new_str[i] = neu_str;
                    
                }
                MessageBox.Show("Die Ergebnisse der beiden Methoden sind unterschiedlich.\n" +
                    "Vorne:\n       Methode1: "+Ergebnis_trad_str[0]+"\n       Methode2: " + Ergebnis_new_str[0]+
                    "\n            T/N=(M1:)" + Math.Round(Ergebnis_trad_sum[0], 3) + "<=>(M2:)"+ Math.Round(Bestratio_new2[0], 3) +
                    "\nHinten:\n        Methode1: "+Ergebnis_trad_str[1]+"\n       Methode2: " + Ergebnis_new_str[1] +
                    "\n            T/N=(M1:)" + Math.Round(Ergebnis_trad_sum[1],3) + "<=>(M2:)" + Math.Round(Bestratio_new2[1],3)
                    , "Achtung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (Bestratio_new2[0] > Ergebnis_trad_sum[0] || Bestratio_new2[1] > Ergebnis_trad_sum[1])
                {
                    RowNumberInExcel = new List<int[]>(Ergebnis_new);
                }
                else
                {
                    RowNumberInExcel = new List<int[]>(Ergebnis_trad);
                }
            }
            
        }
        /// <summary>
        /// By providing the list of Rods, it generates Rod pairs and places them in a Dictionary called "rodPairs" 
        /// key=commonPoint, value=(Left rod, Right rod)
        /// </summary>
        /// <param name="selectedMemmbers"></param>
        public void ShortOfMembers(List<int> selectedMemmbers)
        {
            try
            {
                IModelData data=null;//?
                Node[] Pontok=null;
                Member[] allMembers = null;
                if (!IsConnected()) { return ; }
                connectionManager.LockAndUnlockLicense(() =>
                {
                    
                    //Dlubal.RSTAB8.IModel model = app.GetActiveModel();
                    data = model.GetModelData();
                    Pontok = data.GetNodes();
                    allMembers = data.GetMembers();
                   
                });
                List<Node> selected_Points = new List<Node>();
                List<Member> selected_Members = new List<Member>();
                List<int> NodeNo = new List<int>();
                Member currMember = new Member();
                Node commonPoint = new Node();
                Node outerPoint = new Node();
                supportParts = new Dictionary<Node, Member[]>();
                Member left = new Member();
                Member right = new Member();
                bool pointAlreadyExists = false;
                foreach (var curr_Member in selectedMemmbers)
                {
                    currMember = allMembers[FindIndex(allMembers, curr_Member)];
                    double start = Pontok[FindIndex(Pontok, currMember.StartNodeNo)].Z;
                    double end = Pontok[FindIndex(Pontok, currMember.EndNodeNo)].Z;
                    if (start < end)
                    {
                        commonPoint = Pontok[FindIndex(Pontok, currMember.StartNodeNo)];
                        outerPoint = Pontok[FindIndex(Pontok, currMember.EndNodeNo)];
                    }
                    else
                    {
                        commonPoint = Pontok[FindIndex(Pontok, currMember.EndNodeNo)];
                        outerPoint = Pontok[FindIndex(Pontok, currMember.StartNodeNo)];
                    }
                    foreach (var item in supportParts)
                    {
                        if (supportParts != null && item.Key.No == commonPoint.No)
                        {
                            pointAlreadyExists = true;
                            break;
                        }
                    }
                    if (pointAlreadyExists)
                    {
                        pointAlreadyExists = false;
                        continue;
                    }
                    if (commonPoint.Y < outerPoint.Y)
                    {
                        left = currMember;
                    }
                    else
                    {
                        right = currMember;
                    }
                    foreach (var rodInstance in allMembers)
                    {
                        if (currMember.No == rodInstance.No)
                        {
                            continue;
                        }
                        if (currMember.StartCrossSectionNo == rodInstance.StartCrossSectionNo)
                        {
                            if (commonPoint.No == rodInstance.StartNodeNo || commonPoint.No == rodInstance.EndNodeNo)
                            {
                                if (left.No == currMember.No)
                                {
                                    supportParts.Add(commonPoint, new Member[] { currMember, rodInstance });
                                }
                                else
                                {
                                    supportParts.Add(commonPoint, new Member[] { rodInstance, currMember });
                                }
                                break;
                            }
                        }
                    }
                }
                Dictionary<Node, Member[]> rodPairs2 = new Dictionary<Node, Member[]>();
                if (supportParts.ElementAt(0).Key.X > supportParts.ElementAt(1).Key.X)
                {
                    rodPairs2.Add(supportParts.ElementAt(1).Key, supportParts.ElementAt(1).Value);
                    rodPairs2.Add(supportParts.ElementAt(0).Key, supportParts.ElementAt(0).Value);
                    supportParts.Clear();
                    supportParts = rodPairs2;
                }
                TDurchmesser = supportParts.Select(x => x.Value[0].Length).ToArray()[0];
                foreach (var item in supportParts)
                {
                    Node Pont1 = Pontok[FindIndex(Pontok, item.Value[0].StartNodeNo)];
                    Node Pont2 = Pontok[FindIndex(Pontok, item.Value[0].EndNodeNo)];
                    deg.Add(Math.Atan(Math.Abs((Pont1.Y - Pont2.Y) / (Pont1.Z - Pont2.Z))) * 180 / Math.PI);
                }
            }
            catch (Exception)
            {
                TheEnd();
            }
        }
        /// <summary>
        /// The loads in the selected load combination are separated into glasten and qlasten
        /// Key: 0 (load number, starting from 0), Value: LC1
        /// </summary>
        /// <param name="ResKomb"></param>
        /// 
        private void G_Q_Lasten(int ResKomb)
        {
            app.LockLicense();
            model = app.GetActiveModel();
            int qindex = -1;

            try
            {

                resultComb = model.GetLoads().GetResultCombination(ResKomb, ItemAt.AtNo).GetData();
                allLFNo_in_LC = resultComb.Definition.Replace(" ", "").Split(new string[] { "or", "+" }, System.StringSplitOptions.RemoveEmptyEntries);
                string[] allLFNo_QisGroupped = resultComb.Definition.Replace(" ", "").Split(new string[] { "+" }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in allLFNo_QisGroupped)
                {
                    if (item.Contains("/p"))
                    { continue; }
                    all_Q_LFNo_inGroups.Add(item.Split(new string[] { "or" }, System.StringSplitOptions.RemoveEmptyEntries));
                }
                
                foreach (var item in allLFNo_in_LC)
                {
                    foreach (var lf in loadcaseList)
                    {
                        
                        if (item.Contains("/p"))
                        {
                            if (lf.Number == item.Replace("/p", ""))
                            {
                                lf.LoadType = "G";
                               
                                g_lastenList.Add(lf);
                            }
                        }
                        else
                        {
                            if (lf.Number == item)
                            {
                                lf.LoadType = "Q";
                                qindex++;
                                lf.QIndex = qindex;
                                q_lastenList.Add(lf);
                            }
                        }
                    }
                }

                gq_lastenList.AddRange(g_lastenList);
                gq_lastenList.AddRange(q_lastenList);
                app.UnlockLicense();
            }
            catch (RstabConnectionException ex)
            {
                app.UnlockLicense();
                MessageBox.Show("nur 1,0-fach Lastfalkombination ist akzeptable");
                logger.Error(ex.Message + ":  " + ex.ErrorType + "  " + ex.InnerException?.StackTrace);
                //HandleStateChange("nur 1,0-fach Lastfalkombination ist akzeptable");
                TheEnd();
            }
        }
        /// <summary>
        /// The cases of the selected result combination are placed into the lastfaelle dictionary.
        /// Key: 0 (index, starting from 0), Value: [LC1, Eigenweight]
        /// </summary>
        /// <param name="ResKomb"></param>
        public void Lastfaelle(int ResKomb)
        {
            app.LockLicense();
            model = app.GetActiveModel();
            LoadCase[] loadsLCo = model.GetLoads().GetLoadCases();
            int SummLF = loadsLCo.Count();
            for (int i = 0; i < loadsLCo.Count(); i++)
            {
                loadcaseList.Add(new MyLoadcase(i, loadCaseByLanguage + loadsLCo[i].Loading.No.ToString(), loadsLCo[i].Description));
            }
            G_Q_Lasten(ResKomb);
            app.UnlockLicense();
        }
        /// <summary>
        /// Collects the support and rod forces from each load case.
        /// The forces corresponding to the permanent loads are already summed up.
        /// </summary>
        public void Get_Forces()
        {
            try
            {

                connectionManager.LockAndUnlockLicense(() =>
                {
                    model = app.GetActiveModel();
                    ICalculation calculation = model.GetCalculation();


                    var forceCalc = new ForceCalculator(model, calculation, angledRod, tangentialSupport, verticalSupport, supportRotateDirection);
                    var forceCalc_for_Gegengewicht = new ForceCalculator(model, calculation, angledRod, tangentialSupport, verticalSupport, supportRotateDirection); ;
                    ErrorInfo[] errorInfos = calculation.Calculate(LoadingType.LoadCaseType, 0);
                    LoadCase[] loadsLCo = model.GetLoads().GetLoadCases();
                    if (errorInfos.Length != 0)
                    {
                        logger.Error("Get_Forces" + errorInfos[0].Description);
                    }

                    foreach (var lc in gq_lastenList)
                    {

                        var forces = forceCalc.CalculateForces(lc);
                        lc.Force.AddRange(forces);

                    }

                    if (gegengewichtIndex.HasValue)
                    {
                        g_lastenList = g_lastenList.ToList();
                        totalG_with_GG = forceCalc.CalculateTotalForces(g_lastenList).ToList();
                        g_lastenList = g_lastenList.Where(x => x.Index != gegengewichtIndex).ToList();
                        GGforce = forceCalc_for_Gegengewicht.CalculateForces(Gegengewicht); //=> to Export
                    }
                    else
                    {
                        totalG_with_GG = forceCalc.CalculateTotalForces(g_lastenList).ToList();
                    }

                    totalG = forceCalc.CalculateTotalForces(g_lastenList).ToList();

                    normal_Forces.Add(new double[] { totalG[0].N, totalG[1].N });
                    tangential_Forces.Add(new double[] { totalG[0].T, totalG[1].T });
                    vertikal_Forces.Add(new double[] { totalG[0].G, totalG[1].G });
                    foreach (var ql in q_lastenList)
                    {
                        normal_Forces.Add(new double[] { ql.Force[0].N, ql.Force[1].N });
                        tangential_Forces.Add(new double[] { ql.Force[0].T, ql.Force[1].T });
                        vertikal_Forces.Add(new double[] { ql.Force[0].G, ql.Force[1].G });
                    }

                });

            }
            catch (Exception)
            {

                TheEnd();
            }
        }
        /// <summary>
        /// Extracts the load combinations corresponding to the absolute maximum tangential force (one load combination for each of the 2 wheel disks).
        /// The indices of the load cases in the load combination are placed into the List<int> Verrollung_TKs.
        /// The maximum tangential forces corresponding to the load combination are placed into the double[] MaxTangentialkraft (one force for each of the 2 wheel disks).
        /// </summary>
        /// <param name="AuflagerNr"></param>
        /// <param name="Ek"></param>
        public void Verrollung_MaxTKs(List<int> AuflagerNr, int Ek)
        {
            try
            {
                app.LockLicense();
                model = app.GetActiveModel();
                ICalculation calculation = model.GetCalculation();
                ResultCombination[] loadsLRe = model.GetLoads().GetResultCombinations();
                ErrorInfo[] errorInfos = calculation.Calculate(LoadingType.ResultCombinationType, Ek);
                if (errorInfos.Length != 0)
                {
                    app.UnlockLicense();
                    logger.Error("Verrollung_MaxTKs" + errorInfos[0].Description);
                }
                IResults results = calculation.GetResults(LoadingType.ResultCombinationType, Ek);
                int n = AuflagerNr.Count;
                NodalSupportForces[] dataf;
                int counter = 0;
                foreach (var item in AuflagerNr)
                {
                    double[] Support_Force = new double[2];
                    counter = AuflagerNr.ToList().IndexOf(item);
                    dataf = results.GetNodalExtremeSupportForces(item, ItemAt.AtNo, true);
                    int absmax, max = 0, min = 1;
                    for (int i = 0; i < dataf.Length; i++)
                    {
                        if (dataf[i].Type == ResultsValueType.MaximumAlongZ)
                        {
                            Support_Force[0] = dataf[i].Forces.Z / 1000;
                            max = i;
                        }
                        if (dataf[i].Type == ResultsValueType.MinimumAlongZ)
                        {
                            Support_Force[1] = dataf[i].Forces.Z / 1000;
                            min = i;
                        }
                    }
                    if (Math.Abs(Support_Force[0]) > Math.Abs(Support_Force[1]))
                    {
                        absmax = max;
                    }
                    else
                    {
                        absmax = min;
                    }
                    int[] relevantTK = (StringToIntList(dataf[absmax].CorrespondingLoading.Remove(0, 3))).ToArray();
                    Verrollung_TKs.Add(relevantTK);
                    MaxTangentialkraft[counter] = Support_Force[absmax];
                }
                app.UnlockLicense();
            }
            catch
            {
                TheEnd();

            }
        }
        public void TheEnd()
        {
            connectionManager.CloseConnection();
        }

    }
}
