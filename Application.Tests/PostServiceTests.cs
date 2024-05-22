using Application.Contracts;
using Application.Dtos.PostsDto;
using Application.Services;
using AutoMapper;
using Domain.Entities;
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


	}
}