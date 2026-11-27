using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Application.Reseaux;

internal static class ReseauxPostsSync
{
	private const string LinkedInActor = "harvestapi~linkedin-company-posts";

	private const string InstagramActor = "apify~instagram-post-scraper";

	internal static async Task<string> RefreshAsync(ApplicationDbContext db, IConfiguration configuration, CancellationToken cancellationToken)
	{
		string token = PersonLookupController.ReadApifyToken(configuration);
		if (string.IsNullOrWhiteSpace(token))
		{
			return "Jeton Apify absent.";
		}
		using var http = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(55)
		};
		Task<string> linkedInTask = RunActor(http, token, LinkedInActor, new
		{
			targetUrls = new[] { "https://www.linkedin.com/company/630756/" },
			maxPosts = ReseauxSociauxLogic.PostsPerNetwork,
			postedLimit = "any",
			includeQuotePosts = false,
			includeReposts = false,
			scrapeReactions = false,
			scrapeComments = false,
			contextCountry = "FR"
		}, cancellationToken);
		Task<string> instagramTask = RunActor(http, token, InstagramActor, new
		{
			username = new[] { "perlerareimmobilier" },
			resultsLimit = ReseauxSociauxLogic.PostsPerNetwork,
			skipPinnedPosts = true,
			dataDetailLevel = "basicData"
		}, cancellationToken);
		string linkedInJson = await Safe(linkedInTask);
		string instagramJson = await Safe(instagramTask);
		var posts = new List<ReseauPost>();
		if (linkedInJson != null)
		{
			posts.AddRange(ReseauxSociauxLogic.ParseLinkedIn(linkedInJson));
		}
		if (instagramJson != null)
		{
			posts.AddRange(ReseauxSociauxLogic.ParseInstagram(instagramJson));
		}
		if (posts.Count == 0 && linkedInJson == null && instagramJson == null)
		{
			return "Publications LinkedIn et Instagram indisponibles.";
		}
		TimeZoneInfo paris = ReseauxSociauxLogic.ParisTimeZone();
		DateTime nowParis = ReseauxSociauxLogic.ToParisUnspecified(DateTime.UtcNow, paris);
		foreach (ReseauPost post in posts)
		{
			DateTime published = ReseauxSociauxLogic.ToParisUnspecified(post.PublishedUtc, paris);
			PublicationsReseaux row = await db.PublicationsReseaux.FirstOrDefaultAsync(
				(PublicationsReseaux p) => p.PubReseau == post.Reseau && p.PubPostId == post.Id,
				cancellationToken);
			if (row == null)
			{
				db.PublicationsReseaux.Add(new PublicationsReseaux
				{
					PubPostId = post.Id,
					PubReseau = post.Reseau,
					PubUrl = post.Url,
					PubDatePublication = published,
					PubVignette = post.Vignette ?? string.Empty,
					PubTexte = post.Texte ?? string.Empty,
					PubMaj = nowParis
				});
			}
			else
			{
				row.PubUrl = post.Url;
				row.PubDatePublication = published;
				row.PubVignette = post.Vignette ?? string.Empty;
				row.PubTexte = post.Texte ?? string.Empty;
				row.PubMaj = nowParis;
			}
		}
		int unix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		AdminVersions stamp = await db.AdminVersions.FirstOrDefaultAsync((AdminVersions v) => v.AvParamName == ReseauxSociauxLogic.SyncParam, cancellationToken);
		if (stamp == null)
		{
			db.AdminVersions.Add(new AdminVersions
			{
				AvParamName = ReseauxSociauxLogic.SyncParam,
				AvVersion = unix
			});
		}
		else
		{
			stamp.AvVersion = unix;
		}
		await db.SaveChangesAsync(cancellationToken);
		await ReseauxEngagementStore.SaveAsync(db, posts, cancellationToken);
		if (linkedInJson == null && instagramJson == null)
		{
			return "Publications indisponibles.";
		}
		if (linkedInJson == null)
		{
			return "Publications LinkedIn indisponibles.";
		}
		if (instagramJson == null)
		{
			return "Publications Instagram indisponibles.";
		}
		return string.Empty;
	}

	private static async Task<string> Safe(Task<string> task)
	{
		try
		{
			return await task;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static async Task<string> RunActor(HttpClient http, string token, string actorId, object input, CancellationToken cancellationToken)
	{
		string url = "https://api.apify.com/v2/acts/" + actorId + "/run-sync-get-dataset-items?timeout=50&memory=512";
		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		request.Content = JsonContent.Create(input);
		using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
		string body = await response.Content.ReadAsStringAsync(cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("Lecture des publications indisponible.");
		}
		return body;
	}
}
