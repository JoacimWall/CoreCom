using System;
using System.ComponentModel.DataAnnotations;

namespace WallTec.CoreCom.Models
{
   internal class TransferStatus
    {
        [Key]
        public int TransferStatusId { get; set; }
        public String Name { get; set; }
    }
}
