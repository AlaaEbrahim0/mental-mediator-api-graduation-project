using System.IdentityModel.Tokens.Jwt;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Infrastructure.Contracts;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.PostsDto;

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

    public async Task<Result<IEnumerable<PostResponse>>> GetPosts(
        RequestParameters parameters)
    {
        var posts = await _repos.Posts.GetAllPosts(parameters, false);
        var postResponse = _mapper.Map<IEnumerable<PostResponse>>(posts);
        return postResponse.ToList();
    }

    public async Task<Result<PostResponse?>> GetPostById(int id)
    {
        var post = await _repos.Posts.GetPostById(id, false);
        if (post is null)
        {
            return PostErrors.NotFound(id);
        }
        var postResponse = _mapper.Map<PostResponse>(post);
        return postResponse;
    }

    private string GetUserId()
    {
        return _signInManager.Context.User.FindFirst("uid")?.Value!;
    }
    private string GetUserName()
    {
        var userName = _signInManager.Context.User.FindFirst(JwtRegisteredClaimNames.Name)!.Value;
        return userName;
    }

    public async Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest postRequest)
    {
        var userId = GetUserId();
        var userName = GetUserName();
        var post = _mapper.Map<Post>(postRequest);

        post.AppUserId = userId;
        post.PostedOn = DateTime.UtcNow;
        post.Username = userName;

        _repos.Posts.CreatePost(post);
        await _repos.SaveAsync();

        var postResponse = _mapper.Map<PostResponse>(post);
        return postResponse;
    }

    public async Task<Result<PostResponse>> DeletePost(int id)
    {
        var userId = GetUserId();
        var post = await _repos.Posts.GetPostById(id, true);
        if (post is null)
        {
            return PostErrors.NotFound(id);
        }
        if (!post.AppUserId!.Equals(userId))
        {
            return PostErrors.Forbidden(post.Id);
        }

        _repos.Posts.DeletePost(post);
        await _repos.SaveAsync();

        var postResponse = _mapper.Map<PostResponse>(post);
        return postResponse;

    }

    public async Task<Result<PostResponse>> UpdatePost(int id, UpdatePostRequest updatePostRequest)
    {
        var userId = GetUserId();
        var post = await _repos.Posts.GetPostById(id, true);

        if (post is null)
        {
            return PostErrors.NotFound(id);
        }
        if (!post.AppUserId!.Equals(userId))
        {
            return PostErrors.Forbidden(id);
        }

        _mapper.Map(updatePostRequest, post);
        _repos.Posts.UpdatePost(post);
        await _repos.SaveAsync();

        var postResponse = _mapper.Map<PostResponse>(post);
        return postResponse;
    }




}