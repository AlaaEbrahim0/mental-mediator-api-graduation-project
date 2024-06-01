using Application.Contracts;
using Application.Dtos.PostsDto;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Moq;
using Shared;

namespace Application.Tests;

public class PostServiceTests
{
	private readonly PostService _sut;
	private readonly Mock<IRepositoryManager> _mockRepos;
	private readonly Mock<IMapper> _mockMapper;
	private readonly Mock<IHateSpeechDetector> _mockHateSpeechDetector;
	private readonly Mock<IUserClaimsService> _mockUserClaimsService;
	private readonly Mock<ICacheService> _mockCacheService;

	public PostServiceTests()
	{
		_mockCacheService = new Mock<ICacheService>();
		_mockMapper = new Mock<IMapper>();
		_mockHateSpeechDetector = new Mock<IHateSpeechDetector>();
		_mockUserClaimsService = new Mock<IUserClaimsService>();
		_mockRepos = new Mock<IRepositoryManager>();

		_sut = new PostService(
			_mockRepos.Object,
			_mockMapper.Object,
			_mockUserClaimsService.Object,
			_mockHateSpeechDetector.Object,
			_mockCacheService.Object
		);
	}


	[Fact]
	public async Task GetPosts_ReturnsCachedPosts_WhenCacheIsNotEmpty()
	{
		// Arrange
		var parameters = new RequestParameters
		{
			PageNumber = 1,
			PageSize = 1
		};
		string postPageKey = $"posts_{parameters.PageNumber}_{parameters.PageSize}";
		var cachedPosts = new List<PostResponse> { new PostResponse { Title = "string" } };
		_mockCacheService.Setup(x => x.GetAsync<List<PostResponse>>(postPageKey))
			.ReturnsAsync(cachedPosts);

		// Act
		var result = await _sut.GetPosts(parameters);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.NotNull(result.Value);
		Assert.Equal(cachedPosts, result.Value);
		_mockRepos.Verify(x => x.Posts.GetAllPosts(parameters, false), Times.Never);
		_mockCacheService.Verify(x => x.SetAsync(postPageKey, It.IsAny<IEnumerable<PostResponse>>(), It.IsAny<TimeSpan>()), Times.Never);
	}

	[Fact]
	public async Task GetPosts_ReturnsPostsFromRepo_WhenCacheIsEmpty()
	{
		// Arrange
		var parameters = new RequestParameters
		{
			PageNumber = 1,
			PageSize = 1
		};
		string postPageKey = $"posts_{parameters.PageNumber}_{parameters.PageSize}";
		var posts = new List<Post> { new Post() };
		var postResponses = new List<PostResponse> { new PostResponse() };

		_mockCacheService.Setup(x => x.GetAsync<List<PostResponse>>(postPageKey))
			.ReturnsAsync((List<PostResponse>)null!);

		_mockRepos.Setup(x => x.Posts.GetAllPosts(parameters, false))
			.ReturnsAsync(posts);

		_mockMapper.Setup(x => x.Map<IEnumerable<PostResponse>>(posts))
			.Returns(postResponses);

		// Act
		var result = await _sut.GetPosts(parameters);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(result.Value, postResponses);
		_mockCacheService.Verify(x => x.GetAsync<List<PostResponse>>(postPageKey), Times.Once);
		_mockRepos.Verify(x => x.Posts.GetAllPosts(parameters, false), Times.Once);
		_mockMapper.Verify(x => x.Map<IEnumerable<PostResponse>>(posts), Times.Once);
		_mockCacheService.Verify(x => x.SetAsync(postPageKey, result.Value, TimeSpan.FromMinutes(1)), Times.Once);

	}

	[Fact]
	public async Task GetPosts_ReturnsEmptyList_WhenRepoReturnsNoPosts()
	{
		// Arrange
		var parameters = new RequestParameters { PageNumber = 1, PageSize = 10 };
		var posts = new List<Post>();
		var postResponses = new List<PostResponse>();
		string postPageKey = $"posts_{parameters.PageNumber}_{parameters.PageSize}";

		_mockCacheService.Setup(x => x.GetAsync<List<PostResponse>>(postPageKey))
			.ReturnsAsync((List<PostResponse>)null!);
		_mockRepos.Setup(x => x.Posts.GetAllPosts(parameters, false))
			.ReturnsAsync(posts);
		_mockMapper.Setup(x => x.Map<IEnumerable<PostResponse>>(posts))
			.Returns(postResponses);

		// Act
		var result = await _sut.GetPosts(parameters);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Empty(result.Value);
	}





	[Fact]
	public async Task GetPostById_ReturnsNotFound_WhenPostDoesntExist()
	{
		// Arrange
		int postId = -1;
		_mockRepos.Setup(x => x.Posts.GetPostById(postId, false))
			.ReturnsAsync(default(Post));

		// Act
		var response = await _sut.GetPostById(postId);

		// Assert
		Assert.True(response.IsFailure);
		Assert.Equal(response.Error, PostErrors.NotFound(postId));

	}

	[Fact]
	public async Task GetPostById_ReturnsPostResponse_WhenPostDoesntExist()
	{
		// Arrange
		int postId = 1;
		var post = new Post
		{
			Id = postId,
			Content = "string",
			Title = "string",
			PostedOn = DateTime.UtcNow,
		};
		var postResponse = new PostResponse
		{
			Id = postId,
			Content = "string",
			Title = "string",
			PostedOn = DateTime.UtcNow,
		};
		_mockRepos.Setup(x => x.Posts.GetPostById(postId, false))
			.ReturnsAsync(post);

		_mockMapper.Setup(x => x.Map<PostResponse>(post))
			.Returns(postResponse);

		// Act
		var response = await _sut.GetPostById(postId);

		// Assert
		Assert.True(response.IsSuccess);
		Assert.Equal(response.Value, postResponse);
	}

	[Fact]
	public async Task CreatePostAsync_ReturnsServiceUnavailable_WhenHateSpeechDetectorServiceIsUnavailable()
	{
		// Arrange
		var postRequest = new CreatePostRequest
		{
			Content = "content",
			Title = "title",
		};
		var hateSpeechResult = Error.ServiceUnavailable("ExternalServices.HateSpeechDetectionServiceUnavailable", "failed to fetch data from ml server");

		var text = postRequest.Title + " " + postRequest.Content;
		_mockHateSpeechDetector.Setup(x => x.IsHateSpeech(text))
			.ReturnsAsync(hateSpeechResult);

		// Act
		var result = await _sut.CreatePostAsync(postRequest);

		// Assert
		Assert.True(result.IsFailure);
		Assert.Equal(result.Error, hateSpeechResult);
	}
	[Fact]
	public async Task CreatePostAsync_ReturnsForbidden_WhenHateSpeechDetected()
	{
		// Arrange
		var postRequest = new CreatePostRequest
		{
			Content = "bad content",
			Title = "bad title",
		};
		var hateSpeechResult = Error.Forbidden("Content.Forbidden", "Your post violates our policy against hate speech and could not be published");

		var text = postRequest.Title + " " + postRequest.Content;
		_mockHateSpeechDetector.Setup(x => x.IsHateSpeech(text))
			.ReturnsAsync(hateSpeechResult);

		// Act
		var result = await _sut.CreatePostAsync(postRequest);

		// Assert
		Assert.True(result.IsFailure);
		Assert.Equal(result.Error, hateSpeechResult);
	}
	[Fact]
	public async Task CreatePostAsync_ReturnsAnonymousPostResponse_WhenIsAnonymousTrueAndHateSpeechIsNotDetected()
	{
		// Arrange
		var postRequest = new CreatePostRequest
		{
			Content = "good content",
			Title = "good title",
			IsAnonymous = true
		};

		var text = postRequest.Content + " " + postRequest.Title;
		var isHateSpeechResult = Result<bool>.Success(false);

		_mockHateSpeechDetector.Setup(x => x.IsHateSpeech(text))
			.ReturnsAsync(isHateSpeechResult);

		var post = new Post
		{
			Content = postRequest.Content,
			Title = postRequest.Title,
			IsAnonymous = postRequest.IsAnonymous,
			AppUserId = "user-id",
			PostedOn = DateTime.UtcNow,
		};

		var postResponse = new PostResponse
		{
			Content = post.Content,
			Title = post.Title,
			IsAnonymous = true,
			PostedOn = post.PostedOn,
			AppUserId = post.AppUserId,
			Username = null!
		};

		_mockMapper.Setup(mapper => mapper.Map<Post>(postRequest))
			.Returns(post);

		_mockRepos.Setup(repo => repo.Posts.CreatePost(post));

		_mockRepos.Setup(repo => repo.SaveAsync());

		_mockMapper.Setup(mapper => mapper.Map<PostResponse>(post))
			.Returns(postResponse);

		_mockUserClaimsService.Setup(x => x.GetUserId())
			.Returns("user-id");

		var result = await _sut.CreatePostAsync(postRequest);

		Assert.True(result.IsSuccess);
		Assert.Equal(postResponse, result.Value);

		_mockRepos.Verify(x => x.Posts.CreatePost(post), Times.Once);
		_mockRepos.Verify(x => x.SaveAsync(), Times.Once);
	}

}