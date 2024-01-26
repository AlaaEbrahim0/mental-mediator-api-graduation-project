using Domain.Entities;
using Shared;
using Shared.CommentsDtos;

namespace Application.Services;
public interface ICommentService
{
    Task<Result<IEnumerable<CommentResponse>>> GetCommentsByPostId(int postId);
    Task<Result<string>> DeleteComment(int postId, int commentId);
    Task<Result<string>> CreateComment(int postId, CreateCommentRequest createCommentRequest);
    Task<Result<string>> UpdateComment(int postId, int commentId, UpdateCommentRequest createCommentRequest);
}


