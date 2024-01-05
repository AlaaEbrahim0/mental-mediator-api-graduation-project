using System.ComponentModel.DataAnnotations;

namespace Shared;

public class DeletePostRequest
{
    [Required(ErrorMessage = "Id is required")]
    public int Id { get; set; }
}

