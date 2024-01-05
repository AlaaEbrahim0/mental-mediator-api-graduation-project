using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions;
using Domain.Entities;
using Shared;

namespace Application.Services;
public interface IPostService
{
    Task<Result<IEnumerable<ReadPostResponse>>> GetPosts();
    Task<Result<ReadPostResponse?>> GetPostById(int id);
    Task<Result<string>> UpdatePost(int id, UpdatePostRequest updatePostRequest);
    Task<Result<string>> DeletePost(int id);
    Result<CreatePostResponse> CreatePost(CreatePostRequest postRequest);

}
