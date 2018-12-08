using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        #region Block Finder
        static T GetBlock<T> (string name, bool useSubgrids = false) where T : class, IMyTerminalBlock
        {
            if (useSubgrids)
            {
                return (T)gridSystem.GetBlockWithName(name);
            }
            else
            {
                List<T> blocks = GetBlocks<T>(false);
                foreach (T block in blocks)
                {
                    if (block.CustomName == name)
                        return block;
                }
                return null;
            }
        }
        static T GetBlock<T> (bool useSubgrids = false) where T : class, IMyTerminalBlock
        {
            List<T> blocks = GetBlocks<T>(useSubgrids);
            return blocks.FirstOrDefault();
        }
        static List<T> GetBlocks<T> (bool useSubgrids = false) where T : class, IMyTerminalBlock
        {
            List<T> blocks = new List<T>();
            gridSystem.GetBlocksOfType(blocks);
            if (!useSubgrids)
                blocks.RemoveAll(block => block.CubeGrid.EntityId != gridId);
            return blocks;
        }
        #endregion

    }
}
