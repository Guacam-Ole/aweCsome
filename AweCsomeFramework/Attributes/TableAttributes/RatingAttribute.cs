using OfficeDevPnP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.TableAttributes
{
    public class RatingAttribute : Attribute
    {
        public int VotingExperience { get; set; }
        public RatingAttribute(VotingExperience votingExperience )
        {
            VotingExperience = (int)votingExperience;
        }
        public RatingAttribute(int votingExperience)
        {
            VotingExperience = votingExperience;
        }
    }
}
