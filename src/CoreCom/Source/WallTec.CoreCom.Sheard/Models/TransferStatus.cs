using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WallTec.CoreCom.Sheard.Models
{
   internal class TransferStatus
    {
        [Key]
        public int TransferStatusId { get; set; }
        public String Name { get; set; }
    }
}
