﻿using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenTypeAssociation
    {
        [PrimaryKey, AutoIncrement]
        public int MolenTypeAssociationId { get; set; }
        public int MolenDataId { get; set; }
        public int MolenTypeId { get; set; }
    }
}
