namespace UpBeat.Models;

/// <summary>
/// A music track found on a music source.
/// </summary>
/// <param name="Id">Identifier of the track on its source (e.g. the YouTube video ID).</param>
/// <param name="Title">Title of the track.</param>
/// <param name="Author">Author or channel that published the track.</param>
/// <param name="Duration">Duration of the track, or <c>null</c> if unknown (e.g. live streams).</param>
public record Track(string Id, string Title, string Author, TimeSpan? Duration);
