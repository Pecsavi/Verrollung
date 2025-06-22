
using Dlubal.RSTAB8;
using System.Collections.Generic;
using System.Linq;

namespace Verrollungsnachweis
{
    public interface IForceCalculator
    {
        MyForce[] CalculateForces(MyLoadcase lc);
        MyForce[] CalculateTotalForces(List<MyLoadcase> gLastenList);
    }

    public class ForceCalculator : IForceCalculator
    {
        private readonly IModel model;
        private readonly ICalculation calculation;
        private readonly List<int> angledRod;
        private readonly List<int> tangentialSupport;
        private readonly List<int> verticalSupport;
        private readonly int[] supportRotateDirection;

        public ForceCalculator(IModel model, ICalculation calculation, List<int> angledRod, List<int> tangentialSupport, List<int> verticalSupport, int[] supportRotateDirection)
        {
            this.model = model;
            this.calculation = calculation;
            this.angledRod = angledRod;
            this.tangentialSupport = tangentialSupport;
            this.verticalSupport = verticalSupport;
            this.supportRotateDirection = supportRotateDirection;
        }

        public MyForce[] CalculateForces(MyLoadcase lc)
        {
            int LFNr = int.Parse(lc.Number.Substring(2));
            IResults results = calculation.GetResults(LoadingType.LoadCaseType, LFNr);

            double[] normalForce_Item = new double[4];
            double[] tangential_Force_Item = new double[2];
            double[] vertikal_Force_Item = new double[2];

            int curr = 0;
            foreach (var item_Rod in angledRod)
            {
                MemberForces[] dataN = results.GetMemberInternalForces(item_Rod, ItemAt.AtNo, true);
                normalForce_Item[curr] = dataN[0].Forces.X / 1000;
                curr++;
                if (curr == 4) curr = 0;
            }

            int rest = 0;
            foreach (var (item_support, index) in tangentialSupport.Indexel())
            {
                rest = index % 2;
                NodalSupportForces[] dataf = results.GetNodalSupportForces(item_support, ItemAt.AtNo, true);
                tangential_Force_Item[rest] = dataf[0].Forces.Z / 1000 * supportRotateDirection[rest];
            }

            foreach (var (item_Btamasz, index) in verticalSupport.Indexel())
            {
                rest = index % 2;
                NodalSupportForces[] dataf = results.GetNodalSupportForces(item_Btamasz, ItemAt.AtNo, true);
                vertikal_Force_Item[rest] = dataf[0].Forces.Z / 1000;
                
            }

            return new MyForce[]
            {
                new MyForce { N = normalForce_Item[0] + normalForce_Item[1], T = tangential_Force_Item[0], G = vertikal_Force_Item[0] },
                new MyForce { N = normalForce_Item[2] + normalForce_Item[3], T = tangential_Force_Item[1], G = vertikal_Force_Item[1] }
            };
        }

        public MyForce[] CalculateTotalForces(List<MyLoadcase> gLastenList)
        {
            return new MyForce[]
            {
                new MyForce
                {
                    N = gLastenList.Select(x => x.Force[0].N).Sum(),
                    T = gLastenList.Select(x => x.Force[0].T).Sum(),
                    G = gLastenList.Select(x => x.Force[0].G).Sum()
                },
                new MyForce
                {
                    N = gLastenList.Select(x => x.Force[1].N).Sum(),
                    T = gLastenList.Select(x => x.Force[1].T).Sum(),
                    G = gLastenList.Select(x => x.Force[1].G).Sum()
                }
            };
        }
    }
}
