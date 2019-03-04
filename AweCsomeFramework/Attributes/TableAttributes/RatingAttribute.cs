using System;
using AweCsome.Enumerations;

namespace AweCsome.Attributes.TableAttributes
{
    public class RatingAttribute : Attribute
    {
        public int VotingExperience { get; set; }
        public RatingAttribute(VotingExperience votingExperience)
        {
            VotingExperience = (int)votingExperience;
        }
        public RatingAttribute(int votingExperience)
        {
            VotingExperience = votingExperience;
        }
    }
}
