using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Shared;
using Shared.PostsDto;

namespace Application.Services;
public class PostService : IPostService
{
    private readonly IRepositoryManager _repos;
    private readonly IMapper _mapper;
    private readonly IUserClaimsService _userClaimsService;


    public PostService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService)
    {
        _repos = repos;
        _mapper = mapper;
        _userClaimsService = userClaimsService;

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



    public async Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest postRequest)
    {
        var userId = _userClaimsService.GetUserId();
        var userName = _userClaimsService.GetUserName();

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
        var userId = _userClaimsService.GetUserId();

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
        var userId = _userClaimsService.GetUserId();

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
