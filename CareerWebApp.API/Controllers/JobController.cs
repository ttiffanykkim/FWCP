using CareerWebApp.Models.Job;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Threading.Tasks;

namespace CareerWebApp.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobController : ControllerBase
    {
        // Storing the connection string for the PostgreSQL database
        private readonly string _connectionString;

        public JobController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL");
        }

        // Handle HTTP GET request for retrieving a list of job postings
        [HttpGet]
        [Route("jobs")]
        public IActionResult GetJobList()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                // SQL query to select specific columns from the 'jobpostings' table
                string sql = "SELECT id, jobtitle, department, joblocation, createdat FROM jobpostings";
                var jobSummaries = connection.Query<JobSummary>(sql).ToList();
                return Ok(jobSummaries);
            }
        }

        [HttpGet]
        [Route("job/{id}")]
        public IActionResult GetJobByID(int id)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var parameter = new { jobID = id };

                // SQL query to select all columns from the 'jobpostings' table for a specific job ID
                string sql = "SELECT * FROM jobpostings WHERE id = @jobID;";
                var jobPostring = connection.Query<JobPosting>(sql, parameter).SingleOrDefault();

                // SQL query to retrieve questionnaire questions related to the specific job ID
                sql = "SELECT qq.* FROM jobpostings jp INNER JOIN questionnairequestions qq ON qq.id = ANY(jp.questionnaireid) WHERE jp.id = @jobID;";
                var questions = connection.Query<QuestionnaireQuestions>(sql, parameter).ToList();

                // SQL query to retrieve questionnaire answers related to the specific job ID
                sql = "SELECT qa.* FROM jobpostings jp\r\n\tINNER JOIN questionnaireanswers qa ON qa.questionid = ANY(jp.questionnaireid) WHERE jp.id = @jobID;";
                var answers = connection.Query<QuestionnaireAnswers>(sql, parameter);

                // Create a response object to store job details, questions, and answers
                JobPostingAPIReponse jobDetailReponse = new JobPostingAPIReponse();
                jobDetailReponse.Detail = jobPostring;
                jobDetailReponse.Questions = questions;

                // Populate each question with its corresponding answers
                foreach (var question in jobDetailReponse.Questions)
                {
                    question.Answers = answers.Where(o => o.QuestionID == question.ID).ToList();
                }

                return Ok(jobDetailReponse);
            }
        }
    }
}