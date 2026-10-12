using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.MyProject.Migrations
{
    /// <inheritdoc />
    public partial class Add_Toeic_Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToeicExams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicExams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToeicExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeadlineTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmissionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    WrongCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    Part5Score = table.Column<int>(type: "int", nullable: false),
                    Part6Score = table.Column<int>(type: "int", nullable: false),
                    Part7Score = table.Column<int>(type: "int", nullable: false),
                    AccuracyPercentage = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicExamAttempts_ToeicExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ToeicExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToeicQuestionGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Part = table.Column<int>(type: "int", nullable: false),
                    GroupOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PassageType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PassageText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrls = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicQuestionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicQuestionGroups_ToeicExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ToeicExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToeicQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    QuestionGroupId = table.Column<int>(type: "int", nullable: true),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    Part = table.Column<int>(type: "int", nullable: false),
                    Stem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectOption = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicQuestions_ToeicExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ToeicExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToeicQuestions_ToeicQuestionGroups_QuestionGroupId",
                        column: x => x.QuestionGroupId,
                        principalTable: "ToeicQuestionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToeicAnswerOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionKey = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicAnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicAnswerOptions_ToeicQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "ToeicQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToeicAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    SelectedOption = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    IsFlagged = table.Column<bool>(type: "bit", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToeicAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToeicAttemptAnswers_ToeicExamAttempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "ToeicExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToeicAttemptAnswers_ToeicQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "ToeicQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAnswerOptions_QuestionId_OptionKey",
                table: "ToeicAnswerOptions",
                columns: new[] { "QuestionId", "OptionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptAnswers_ExamAttemptId_QuestionId",
                table: "ToeicAttemptAnswers",
                columns: new[] { "ExamAttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicAttemptAnswers_QuestionId",
                table: "ToeicAttemptAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExamAttempts_ExamId",
                table: "ToeicExamAttempts",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicExams_Code",
                table: "ToeicExams",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestionGroups_ExamId",
                table: "ToeicQuestionGroups",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_ExamId_QuestionNumber",
                table: "ToeicQuestions",
                columns: new[] { "ExamId", "QuestionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToeicQuestions_QuestionGroupId",
                table: "ToeicQuestions",
                column: "QuestionGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToeicAnswerOptions");

            migrationBuilder.DropTable(
                name: "ToeicAttemptAnswers");

            migrationBuilder.DropTable(
                name: "ToeicExamAttempts");

            migrationBuilder.DropTable(
                name: "ToeicQuestions");

            migrationBuilder.DropTable(
                name: "ToeicQuestionGroups");

            migrationBuilder.DropTable(
                name: "ToeicExams");
        }
    }
}
