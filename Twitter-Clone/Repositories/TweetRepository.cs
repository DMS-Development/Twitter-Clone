using Microsoft.EntityFrameworkCore;
using Twitter_Clone.Data;
using Twitter_Clone.Interfaces;
using Twitter_Clone.Models.RegularDTOs;
using Twitter_Clone.Services;

namespace Twitter_Clone.Repositories;

public class TweetRepository : ITweetRepository
{
    private readonly TwitterCloneDb _context;
    private readonly TweetMapper _tweetMapper;
    private readonly ILogger<TweetRepository> _logger;

    public TweetRepository(TwitterCloneDb context, TweetMapper tweetMapper, ILogger<TweetRepository> logger)
    {
        _context = context;
        _tweetMapper = tweetMapper;
        _logger = logger;
    }

    public async Task<TweetDto> CreateTweet(TweetDto tweet)
    {
        try
        {
            var user = await _context.Users.FindAsync(tweet.UserId);
            if (user == null)
            {
                _logger.LogError($"User with ID {tweet.UserId} does not exist");
                throw new ArgumentException("User does not exist");
            }

            var newTweet = _tweetMapper.MapTweetDtoToTweet(tweet);

            await _context.Tweets.AddAsync(newTweet);
            await _context.SaveChangesAsync();

            return _tweetMapper.MapTweetToTweetDto(newTweet);
        }
        catch (DbUpdateException e)
        {
            _logger.LogError($"Database Update Error: {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError($"Generic Error: {e.Message}");
            throw;
        }
    }

    public async Task<TweetDto> GetTweetById(int id)
    {
        try
        {
            var tweet = await _context.Tweets.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
            if (tweet == null)
            {
                _logger.LogWarning($"Tweet with ID {id} does not exist");
                return null;
            }

            return _tweetMapper.MapTweetToTweetDto(tweet);
        }
        catch (Exception e)
        {
            _logger.LogError($"Generic Error: {e.Message}");
            throw;
        }
    }


    public async Task<List<TweetDto>> GetAllTweets()
    {
        try
        {
            var tweets = await _context.Tweets.Include(t => t.User).ToListAsync();

            List<TweetDto> tweetDtos = new();

            foreach (var tweet in tweets) tweetDtos.Add(_tweetMapper.MapTweetToTweetDto(tweet));

            return tweetDtos;
        }
        catch (Exception e)
        {
            _logger.LogError($"Generic Error: {e.Message}");
            throw;
        }
    }

    public async Task<List<TweetDto>> GetTweetsByUserId(int userId)
    {
        try
        {
            var tweets = await _context.Tweets.Where(t => t.UserId == userId).Include(t => t.User).ToListAsync();

            List<TweetDto> tweetDtos = new();

            foreach (var tweet in tweets) tweetDtos.Add(_tweetMapper.MapTweetToTweetDto(tweet));

            return tweetDtos;
        }
        catch (Exception e)
        {
            _logger.LogError($"Generic Error: {e.Message}");
            throw;
        }
    }

    public async Task<List<TweetDto>> GetTweetsByFollowingUsers(int userId)
    {
        var tweetDtos = new List<TweetDto>();

        var following = await _context.UserFollows.Where(uf => uf.FollowerId == userId).Select(uf => uf.FollowingId).ToListAsync();

        var tweets = await _context.Tweets.Where(x => following.Contains(x.UserId)).OrderByDescending(x => x.CreatedAt).ToListAsync();

        tweets.ForEach(tweet =>
        {
            var tweetDto = _tweetMapper.MapTweetToTweetDto(tweet);
            tweetDtos.Add(tweetDto);
        });

        return tweetDtos;
    }

    public async Task DeleteTweet(int id)
    {
        try
        {
            var tweet = await _context.Tweets.FindAsync(id);
            if (tweet == null)
            {
                _logger.LogWarning($"Tweet with ID {id} does not exist");
                throw new ArgumentException("Tweet does not exist");
            }

            _context.Tweets.Remove(tweet);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            _logger.LogError($"Database Update Error: {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError($"Generic Error: {e.Message}");
            throw;
        }
    }

}