using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WallTec.CoreCom.Server.Models
{
   public class TransferStatus
    {
        [Key]
        public int TransferStatusId { get; set; }
        public String Name { get; set; }
    }
}
