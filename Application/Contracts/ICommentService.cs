using Application.Dtos.CommentsDtos;
using Shared;

namespace Application.Services;
public interface ICommentService
{
    Task<Result<CommentResponse>> GetCommentById(int postId, int commentId);
    Task<Result<IEnumerable<CommentResponse>>> GetCommentsByPostId(int postId);
    Task<Result<CommentResponse>> DeleteComment(int postId, int commentId);
    Task<Result<CommentResponse>> CreateComment(int postId, CreateCommentRequest createCommentRequest);
    Task<Result<CommentResponse>> UpdateComment(int postId, int commentId, UpdateCommentRequest createCommentRequest);
}


