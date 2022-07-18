// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using LEGOMaterials;

namespace LEGOModelImporter
{

    public class Tube : CommonPart
    {
        public List<int> connections = new List<int>();
        public PlanarField field;

        public override bool IsVisible()
        {
            if(!field || field.connections == null) return true;
            foreach (var connectionIndex in connections)
            {                
                var connection = field.connections[connectionIndex];
                // FIXME Temporary fix to tube removal while we work on connections that are related/non-rejecting but not connected.
                // FIXME This fix allows for tubes to work without reimporting existing models. Should be removed at a later state.
                if (connection.IsRelevantForTube())
                {
                    if(!connection.HasConnection())
                    {
                        return true;
                    }
                    var connectedTo = field.GetConnection(connectionIndex);

                    var notCovering = (connection.flags & PlanarFeature.flagsCoveringTube) == 0 || (connectedTo.flags & PlanarFeature.flagsCoveringTube) == 0;
                    notCovering |= MouldingColour.IsAnyTransparent(part.materialIDs) || MouldingColour.IsAnyTransparent(connectedTo.field.connectivity.part.materialIDs);

                    if (notCovering)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}


