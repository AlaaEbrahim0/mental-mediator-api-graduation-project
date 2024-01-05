using Application.Abstractions;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Contracts;
using Microsoft.AspNetCore.Identity;
using Shared;

namespace Infrastructure.Services;
public class PostService : IPostService
{
    private readonly IRepositoryManager _repos;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IMapper _mapper;

    public PostService(IMapper mapper, SignInManager<AppUser> signInManager, IRepositoryManager repos)
    {
        _mapper = mapper;
        _signInManager = signInManager;
        _repos = repos;
    }

    public async Task<Result<IEnumerable<ReadPostResponse>>> GetPosts()
    {
        var posts = await _repos.Posts.GetAllPosts(false);
        if (!posts.Any())
        {
            return new Error("Posts.PostsNotFound", "No posts were found");
        }
        var postResponse = _mapper.Map<IEnumerable<ReadPostResponse>>(posts);
        return postResponse.ToList();
    }

    public async Task<Result<ReadPostResponse?>> GetPostById(int id)
    {
        var post = await _repos.Posts.GetPostById(id, false);
        if (post is null)
        {
            return new Error("Posts.PostNotFound", "post doesn'st exist");
        }
        var postResponse = _mapper.Map<ReadPostResponse>(post);
        return postResponse;
    }

    private string GetUserId()
    {
        return _signInManager.Context.User.FindFirst("uid")?.Value!;
    }

    public Result<CreatePostResponse> CreatePost(CreatePostRequest postRequest)
    {
        var userId = GetUserId();
        var post = _mapper.Map<Post>(postRequest);

        post.AppUserId = userId;
        post.PostedOn = DateTime.UtcNow;

        _repos.Posts.CreatePost(post);
        _repos.SaveAsync();

        return new CreatePostResponse()
        {
            Id = post.Id,
            Message = "post was created successfully"
        };
    }


    public async Task<Result<string>> DeletePost(int id)
    {
        var userId = GetUserId();
        var post = await _repos.Posts.GetPostById(id, true);
        if (post is null)
        {
            return new Error("Posts.NotFound", "post doesn't exist");
        }
        if (!post.AppUserId!.Equals(userId))
        {
            return new Error("Posts.UnauthorizedDeletion", "you can't delete a post that you didn't publish");
        }

        _repos.Posts.DeletePost(post);
        await _repos.SaveAsync();
        return "post has been deleted successfully";
    }

    public async Task<Result<string>> UpdatePost(int id, UpdatePostRequest updatePostRequest)
    {
        var userId = GetUserId();
        var post = await _repos.Posts.GetPostById(id, true);

        if (post is null)
        {
            return new Error("Posts.NotFound", "post doesn't exist");
        }
        if (!post.AppUserId!.Equals(userId))
        {
            return new Error("Posts.UnauthorizedUpdation", "you can't update a post that you didn't publish");
        }

        _mapper.Map(updatePostRequest, post);
        _repos.Posts.UpdatePost(post);
        await _repos.SaveAsync();

        return "post has been updated successfully";
    }

}