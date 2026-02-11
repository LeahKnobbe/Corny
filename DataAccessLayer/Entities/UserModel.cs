using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    public class UserModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        //name
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        //email
        public required string Email { get; set; }

        //pasword
        public required string Password { get; set; }  

        //bday

        public DateOnly Bday { get; set; }
    }
}
