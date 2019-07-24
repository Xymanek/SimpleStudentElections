using System.Linq;
using AutoMapper;
using AutoMapper.EquivalencyExpression;
using SimpleStudentElections.Helpers;
using SimpleStudentElections.Models;
using SimpleStudentElections.Models.Forms;

namespace SimpleStudentElections
{
    public static class AutoMapperConfig
    {
        /// <summary>
        /// Configures mapping for the AutoMapper library
        /// </summary>
        public static void InitializeAutoMapper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddCollectionMappers();

                cfg.CreateMap<Election, ElectionForm>()
                    .ReverseMap()
                    .ForMember(election => election.Id, op => op.Ignore()); // Do not touch the ID

                // Election phases

                cfg.CreateMap<ElectionPhaseBase, ElectionPhaseForm>()
                    .ReverseMap();

                cfg.CreateMap<NominationPhase, ElectionPhaseForm>()
                    .IncludeBase<ElectionPhaseBase, ElectionPhaseForm>()
                    .ReverseMap()
                    .IncludeBase<ElectionPhaseForm, ElectionPhaseBase>();
                
                cfg.CreateMap<VotingPhase, ElectionPhaseForm>()
                    .IncludeBase<ElectionPhaseBase, ElectionPhaseForm>()
                    .ReverseMap()
                    .IncludeBase<ElectionPhaseForm, ElectionPhaseBase>();

                // Council-specific

                cfg.CreateMap<Election, CouncilElectionForm>()
                    .IncludeBase<Election, ElectionForm>()
                    .ForMember(form => form.Roles, op => op.MapFrom(election => election.Positions))
                    .AfterMap((election, form) => form.Roles = form.Roles.OrderBy(role => role.Id).ToList()) // Fix order

                    .ReverseMap()
                    .IncludeBase<ElectionForm, Election>()

                    // Set the election reference for new elections
                    .AfterMap((form, election) => election.Positions.ForEach(position => position.Election = election));

                cfg.CreateMap<CouncilElectionData, CouncilElectionForm>()
                    .ReverseMap();

                // Council roles

                cfg.CreateMap<VotablePosition, CouncilRoleForm>()
                    .EqualityComparison((position, form) => position.Id == form.Id)
                    .ForMember(form => form.Name, op => op.MapFrom(position => position.HumanName))

                    .ReverseMap()
                    .EqualityComparison((form, position) => form.Id == position.Id)
                    .ForMember(position => position.Id, op => op.Ignore()); // Do not touch the ID

                // Emails

                cfg.CreateMap<EmailDefinition, EmailForm>()
                    .ReverseMap();
            });
        }
    }
}