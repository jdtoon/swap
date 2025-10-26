using System.ComponentModel.DataAnnotations;

namespace Swap.CLI.Models;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    
    public string Name { get; set; }
    
    [Required]
    
    public decimal Price { get; set; }
    
    [Required]
    
    public string SKU { get; set; }
    
    [Required]
    
    public bool InStock { get; set; }
    
    [Required]
    
    public DateTime CreatedDate { get; set; }
}
