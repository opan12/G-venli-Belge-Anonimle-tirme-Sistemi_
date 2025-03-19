using Microsoft.AspNetCore.Mvc;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using System;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ArticleController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadArticle([FromForm] ArticleUploadModel model)
        {
            if (model.PdfFile == null || model.PdfFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // "uploads" klasörünü oluştur
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            var fileExtension = Path.GetExtension(model.PdfFile.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}"; // Rastgele dosya adı oluştur
            var filePath = Path.Combine(uploadDirectory, fileName); // Tam dosya yolu

            // Dosyayı kaydet
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.PdfFile.CopyToAsync(fileStream);
            }

            // NLP ile alan belirleme
            var articleArea = DetermineArticleAreas(filePath);

            // Veritabanına TAM DOSYA YOLUNU kaydet
            var article = new Makale
            {
                AuthorEmail = model.AuthorEmail,
                ContentPath = filePath, // TAM DOSYA YOLU KAYDEDİLİYOR
                TrackingNumber = Guid.NewGuid().ToString(),
                Status = "Uploaded",
                Content = "",
                AnonymizedContent = "",
                ArticleDate = DateTime.Now,
                Alan = string.Join(", ", articleArea)
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return Ok(new { TrackingNumber = article.TrackingNumber, FilePath = filePath });
        }


        [HttpGet("status/{trackingNumber}")]
        public async Task<IActionResult> GetArticleStatus(string trackingNumber,string email)
        {
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.TrackingNumber == trackingNumber && a.AuthorEmail== email);

            if (article == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            return Ok(new { Status = article.Status });
        }

        [HttpGet("reviews/{articleId}")]
        public async Task<IActionResult> GetReviews(int articleId)
        {
            var reviews = await _context.reviews
                .Where(r => r.MakaleId == articleId)
                .Select(r => new
                {
                    ReviewerId = r.ReviewerId,
                    Comments = r.Comments
                })
                .ToListAsync();

            if (!reviews.Any())
            {
                return NotFound("Bu makale için henüz yorum bulunmamaktadır.");
            }

            return Ok(reviews);
        }

        [HttpPut("revise/{trackingNumber}")]
        public async Task<IActionResult> ReviseArticle(string trackingNumber, [FromForm] ArticleUploadModel model)
        {
            // Find the article by tracking number
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.TrackingNumber == trackingNumber);

            if (article == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            if (model.PdfFile == null || model.PdfFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Read the new file content into a byte array
            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await model.PdfFile.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            // Convert the byte array to a Base64 string
            var base64Content = Convert.ToBase64String(fileContent);

            // Update the article details
            article.ContentPath = base64Content; // Store the new file content as a Base64 string
            article.Status = "Revized"; // Update the status
            article.ArticleDate = DateTime.Now; // Update the revision date

            _context.Articles.Update(article);
            await _context.SaveChangesAsync();

            return Ok(new { TrackingNumber = article.TrackingNumber });
        }
        private List<string> DetermineArticleAreas(string filePath)
        {
            // Read the content of the file
            string fileContent;
            using (var reader = new StreamReader(filePath))
            {
                fileContent = reader.ReadToEnd();
            }

            // Define keywords for each area
            var keywords = new Dictionary<string, List<string>>
{
    { "Artificial Intelligence", new List<string>
        {
            "deep learning", "natural language processing", "computer vision", "generative AI",
            "reinforcement learning", "self-supervised learning", "Bayesian networks", "transformer models",
            "meta-learning", "explainable AI", "federated learning", "quantum AI"
        }
    },

    { "Human-Computer Interaction", new List<string>
        {
            "brain-computer interfaces", "user experience design", "augmented reality", "virtual reality",
            "haptic feedback", "gesture recognition", "adaptive interfaces", "speech recognition",
            "wearable computing", "eye tracking", "tactile interfaces", "affective computing"
        }
    },

    { "Big Data and Data Analytics", new List<string>
        {
            "data mining", "data visualization", "Hadoop", "Spark", "time series analysis",
            "data warehousing", "dimensionality reduction", "stream processing", "clustering algorithms",
            "anomaly detection", "graph analytics", "real-time analytics", "feature engineering", "NoSQL databases"
        }
    },

    { "Cybersecurity", new List<string>
        {
            "encryption algorithms", "secure software development", "network security", "authentication systems",
            "digital forensics", "penetration testing", "zero-trust security", "blockchain security",
            "AI-driven cybersecurity", "identity and access management", "homomorphic encryption", "firewall technologies",
            "secure boot", "quantum cryptography"
        }
    },

    { "Networking and Distributed Systems", new List<string>
        {
            "5G", "cloud computing", "blockchain", "P2P systems", "decentralized systems",
            "edge computing", "content delivery networks", "software-defined networking", "mesh networks",
            "Internet of Things (IoT)", "fog computing", "network slicing", "container orchestration", "serverless computing"
        }
    },

    { "Quantum Computing", new List<string>
        {
            "quantum algorithms", "quantum cryptography", "quantum entanglement", "superconducting qubits",
            "quantum supremacy", "quantum annealing", "Shor's algorithm", "Grover's algorithm",
            "quantum teleportation", "decoherence", "quantum machine learning"
        }
    },

    { "Robotics and Automation", new List<string>
        {
            "autonomous vehicles", "swarm robotics", "robot perception", "robotic vision",
            "industrial automation", "soft robotics", "SLAM (Simultaneous Localization and Mapping)",
            "kinematics and dynamics", "bio-inspired robotics", "exoskeletons", "AI-powered robotics"
        }
    },

    { "Biotechnology and Bioinformatics", new List<string>
        {
            "genome sequencing", "protein structure prediction", "CRISPR", "systems biology",
            "metagenomics", "phylogenetics", "epigenetics", "drug discovery", "molecular docking",
            "synthetic biology", "biological network analysis", "microbiome studies"
        }
    },

    { "Renewable Energy and Sustainability", new List<string>
        {
            "solar energy", "wind energy", "smart grids", "energy storage systems",
            "hydrogen fuel cells", "sustainable architecture", "carbon footprint reduction",
            "wave and tidal energy", "biodegradable materials", "waste-to-energy conversion"
        }
    },

    { "Internet of Things (IoT)", new List<string>
        {
            "smart sensors", "wearable devices", "edge AI", "smart homes",
            "industrial IoT", "real-time monitoring", "LoRaWAN", "Zigbee", "IoT security",
            "embedded systems", "IoT analytics"
        }
    },

    { "Natural Sciences and Mathematics", new List<string>
        {
            "graph theory", "topology", "chaos theory", "dynamical systems",
            "complex systems", "fractals", "number theory", "fluid dynamics",
            "computational physics", "quantum field theory", "astrophysics"
        }
    },

    { "Finance and FinTech", new List<string>
        {
            "cryptocurrency", "blockchain in finance", "decentralized finance (DeFi)", "algorithmic trading",
            "quantitative finance", "risk modeling", "credit scoring", "automated wealth management",
            "digital payments", "regulatory technology (RegTech)", "anti-money laundering (AML)"
        }
    }
};

            // List to hold found areas
            var foundAreas = new List<string>();

            // Check the content for keywords and determine the areas
            foreach (var keyword in keywords)
            {
                foreach (var term in keyword.Value)
                {
                    if (fileContent.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!foundAreas.Contains(keyword.Key))
                        {
                            foundAreas.Add(keyword.Key); // Add the area if a keyword is found
                        }
                        break; // Break out of the term loop to avoid duplicates for the same area
                    }
                }
            }

            return foundAreas.Count > 0 ? foundAreas : new List<string> { "Unknown Area" }; // Return found areas or "Unknown Area"
        }

    }
}