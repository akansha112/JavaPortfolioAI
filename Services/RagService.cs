using PortfolioAI.Models;
using PortfolioAI.Repositories;

namespace PortfolioAI.Services
{
    public class RagService
    {
        private readonly IVectorRepository _vectorRepo;
        private readonly IAIService _ai;
        private readonly ChatHistoryRepository _historyRepo;

        public RagService(IVectorRepository vectorRepo, IAIService ai, ChatHistoryRepository historyRepo)
        {
            _vectorRepo = vectorRepo;
            _ai = ai;
            _historyRepo = historyRepo;
        }

        // -------------------------------
        // CHAT QUESTION ANSWERING
        // -------------------------------
        public async Task<string> AskAsync(string question)
        {
            // Handle simple greetings immediately
            if (new[] { "hi", "hello", "hey" }.Contains(question.ToLower()))
                return "Hello! I am Akansha's AI assistant. You can ask me about her work, projects, and skills.";

            // Step 1: Search vector DB (resume context)
            var docs = await _vectorRepo.SearchAsync(question);

            Console.WriteLine("[DEBUG] Retrieved context for AI prompt:");
            foreach (var doc in docs)
            {
                Console.WriteLine(doc.Content);
            }

            // Step 2: Build context from retrieved chunks
            var context = docs.Count > 0
                ? string.Join("\n", docs.Select((d, i) => $"[{i + 1}] {d.Content}"))
                : "";

            // Step 2b: Add fallback context for general questions
            context += "\n" + GetFallbackContext();

            // Step 3: Construct a robust prompt for AI
            var prompt = $@"
You are the Official AI Portfolio Assistant for Akansha Saxena. 
Today's date is {DateTime.Now:MMMM dd, yyyy}.

### IDENTITY & TONE:
- Speak as a professional representative of Akansha Saxena.
- Use a helpful, confident, and technically sound tone.
- Refer to Akansha in the third person.

### DATA INTERPRETATION RULES:
1. **Current Role (CRITICAL)**: If an experience entry ends with 'Present' (e.g., 'Apr 2025 - Present'), that is her **CURRENT** company and role. Do NOT say you don't have information if 'Present' is visible.
2. **Date Logic**: 
   - If the end date is in the past (e.g., Oct 2024), refer to it as a 'Previous Role' or 'Completed Internship'.
   - If the date is 'Present', it is her active employment.
3. **Education**: If 'Sept 2024' is the end date for B.Tech, she is a Graduate.
4. **Missing Info**: If a question is asked about something NOT in the context (like her favorite color or personal family details), only then say: 'I'm sorry, I don't have that specific information in my professional records.'

### RESUME CONTEXT:
{context}

### USER QUESTION:
{question}

### INSTRUCTIONS FOR THE RESPONSE:
- Provide a direct and clear answer first.
- If the question is 'What is her current company?', look for the 'Present' tag in the Experience section.
- Be detailed about her Tech Stack (.NET 8, React, Kafka, etc.) when relevant.

ANSWER:
";
            // Step 4: Ask AI model
            var answer = await _ai.AskAsync(prompt);

            // Step 5: Save chat history
            var chat = new ChatHistory
            {
                Question = question,
                Answer = answer,
                CreatedAt = DateTime.UtcNow
            };
            await _historyRepo.SaveAsync(chat);

            return answer;
        }

        // ---------------------------------
        // RESUME INDEXING (WITH AUTOMATIC CLEANUP)
        // ---------------------------------
        public async Task IndexResumeAsync(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
                throw new Exception("Resume text is empty");

            // --- NEW: STEP 0: CLEAR OLD DATA ---
            Console.WriteLine("[DEBUG] Initiating cleanup of old vector data...");
            await _vectorRepo.ClearNamespaceAsync();

            // Step 1: Split resume into chunks
            var chunks = SplitIntoChunks(resumeText);
            Console.WriteLine($"[DEBUG] Indexing {chunks.Count} new resume chunks...");

            // Step 2: Store each chunk in vector DB
            foreach (var chunk in chunks)
            {
                await _vectorRepo.UpsertAsync(chunk);
            }

            Console.WriteLine("[DEBUG] Resume indexing completed successfully.");
        }

        public async Task<List<ChatHistory>> GetHistoryAsync()
        {
            return await _historyRepo.GetAllAsync();
        }

        // ---------------------------------
        // HELPER: TEXT CHUNKING
        // ---------------------------------
        private string GetFallbackContext()
        {
            return @"
Akansha Saxena is a professional, friendly software developer.
She can answer questions about her work experience, skills, and projects.
You can greet her or ask basic conversational questions like 'Hello', 'How are you?'
";
        }

        private List<string> SplitIntoChunks(string text)
        {
            int chunkSize = 250; // smaller chunks improve search relevance
            var chunks = new List<string>();

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
            }

            return chunks;
        }
    }
}