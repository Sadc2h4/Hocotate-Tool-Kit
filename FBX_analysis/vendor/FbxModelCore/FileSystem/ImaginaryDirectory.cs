using System.Globalization;
using System.Text.RegularExpressions;

namespace System.IO.Abstractions.TestingHelpers;

using XFS = MockUnixSupport;

/// <inheritdoc />
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryDirectory : DirectoryBase {
  private readonly IImaginaryFileDataAccessor imaginaryFileDataAccessor_;
  private string currentDirectory;

  /// <inheritdoc />
  public ImaginaryDirectory(IImaginaryFileDataAccessor imaginaryFileDataAccessor,
                       FileBase fileBase,
                       string currentDirectory) :
      this(imaginaryFileDataAccessor, currentDirectory) { }

  /// <inheritdoc />
  public ImaginaryDirectory(IImaginaryFileDataAccessor imaginaryFileDataAccessor,
                       string currentDirectory) : base(
      imaginaryFileDataAccessor?.FileSystem) {
    this.currentDirectory = currentDirectory;
    this.imaginaryFileDataAccessor_ =
        imaginaryFileDataAccessor ??
        throw new ArgumentNullException(nameof(imaginaryFileDataAccessor));
  }


  /// <inheritdoc />
  public override IDirectoryInfo CreateDirectory(string path) {
    return this.CreateDirectoryInternal(path);
  }

#if FEATURE_UNIX_FILE_MODE
        /// <inheritdoc />
        public override IDirectoryInfo CreateDirectory(string path, UnixFileMode unixCreateMode)
        {
            throw CommonExceptions.NotImplemented();
        }
#endif

  private IDirectoryInfo CreateDirectoryInternal(string path) {
    if (path == null) {
      throw new ArgumentNullException(nameof(path));
    }

    if (path.Length == 0) {
      throw new ArgumentException(
          StringResources.Manager.GetString(
              "PATH_CANNOT_BE_THE_EMPTY_STRING_OR_ALL_WHITESPACE"),
          "path");
    }

    if (this.imaginaryFileDataAccessor_.ImaginaryPathVerifier.HasIllegalCharacters(
            path,
            true)) {
      throw CommonExceptions.IllegalCharactersInPath(nameof(path));
    }

    path = this.imaginaryFileDataAccessor_.Path.GetFullPath(path).TrimSlashes();
    if (ImaginaryUnixSupport.IsWindowsPlatform()) {
      path = path.TrimEnd(' ');
    }

    var existingFile = this.imaginaryFileDataAccessor_.GetFile(path);
    if (existingFile == null) {
      this.imaginaryFileDataAccessor_.AddDirectory(path);
    } else if (!existingFile.IsDirectory) {
      throw CommonExceptions.FileAlreadyExists("path");
    }

    var created = new ImaginaryDirectoryInfo(this.imaginaryFileDataAccessor_, path);

    return created;
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
        /// <inheritdoc />
        public override IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
        {
          this.imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(path, nameof(path));
          this.imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(pathToTarget, nameof(pathToTarget));

            if (this.Exists(path))
            {
                throw CommonExceptions.FileAlreadyExists(nameof(path));
            }

            this.imaginaryFileDataAccessor_.AddDirectory(path);
            this.imaginaryFileDataAccessor_.GetFile(path).LinkTarget = pathToTarget;

            var directoryInfo =
 new ImaginaryDirectoryInfo(this.imaginaryFileDataAccessor_, path);
            directoryInfo.Attributes |= FileAttributes.ReparsePoint;
            return directoryInfo;
        }
#endif

#if FEATURE_CREATE_TEMP_SUBDIRECTORY
        /// <inheritdoc />
        public override IDirectoryInfo CreateTempSubdirectory(string prefix =
 null)
        {
            prefix ??= "";
            string potentialTempDirectory;

            // Perform directory name generation in a loop, just in case the randomly generated name already exists.
            do
            {
                var randomDir =
 $"{prefix}{this.FileSystem.Path.GetRandomFileName()}";
                potentialTempDirectory =
 Path.Combine(this.FileSystem.Path.GetTempPath(), randomDir);
            } while (this.Exists(potentialTempDirectory));

            return this.CreateDirectoryInternal(potentialTempDirectory);
        }
#endif

  /// <inheritdoc />
  public override void Delete(string path) {
    this.Delete(path, false);
  }


  /// <inheritdoc />
  public override void Delete(string path, bool recursive) {
    path = this.imaginaryFileDataAccessor_.Path.GetFullPath(path).TrimSlashes();

    var stringOps = this.imaginaryFileDataAccessor_.StringOperations;
    var pathWithDirectorySeparatorChar = $"{path}{Path.DirectorySeparatorChar}";

    var affectedPaths = this.imaginaryFileDataAccessor_
                            .AllPaths
                            .Where(p => stringOps.Equals(p, path) ||
                                        stringOps.StartsWith(
                                            p,
                                            pathWithDirectorySeparatorChar))
                            .ToList();

    if (!affectedPaths.Any()) {
      throw CommonExceptions.PathDoesNotExistOrCouldNotBeFound(path);
    }

    if (!recursive && affectedPaths.Count > 1) {
      throw new IOException("The directory specified by " +
                            path +
                            " is read-only, or recursive is false and " +
                            path +
                            " is not an empty directory.");
    }

    bool isFile = !this.imaginaryFileDataAccessor_.GetFile(path).IsDirectory;
    if (isFile) {
      throw new IOException("The directory name is invalid.");
    }

    foreach (var affectedPath in affectedPaths) {
      this.imaginaryFileDataAccessor_.RemoveFile(affectedPath);
    }
  }


  /// <inheritdoc />
  public override bool Exists(string path) {
    if (path == "/") {
      return true;
    }

    try {
      path = path.TrimSlashes();
      path = this.imaginaryFileDataAccessor_.Path.GetFullPath(path);
      return this.imaginaryFileDataAccessor_.GetFile(path)?.IsDirectory ??
             false;
    } catch (Exception) {
      return false;
    }
  }


  /// <inheritdoc />
  public override DateTime GetCreationTime(string path) {
    return this.imaginaryFileDataAccessor_.File.GetCreationTime(path);
  }


  /// <inheritdoc />
  public override DateTime GetCreationTimeUtc(string path) {
    return this.imaginaryFileDataAccessor_.File.GetCreationTimeUtc(path);
  }

  /// <inheritdoc />
  public override string GetCurrentDirectory() {
    return this.currentDirectory;
  }

  /// <inheritdoc />
  public override string[] GetDirectories(string path) {
    return this.GetDirectories(path, "*");
  }

  /// <inheritdoc />
  public override string[] GetDirectories(string path, string searchPattern) {
    return this.GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
  }

  /// <inheritdoc />
  public override string[] GetDirectories(string path,
                                          string searchPattern,
                                          SearchOption searchOption) {
    return this.EnumerateDirectories(path, searchPattern, searchOption).ToArray();
  }

#if FEATURE_ENUMERATION_OPTIONS
        /// <inheritdoc />
        public override string[] GetDirectories(string path, string searchPattern,
            EnumerationOptions enumerationOptions)
        {
            return this.GetDirectories(path, "*", EnumerationOptionsToSearchOption_(enumerationOptions));
        }
#endif

  /// <inheritdoc />
  public override string GetDirectoryRoot(string path) {
    return Path.GetPathRoot(path);
  }

  /// <inheritdoc />
  public override string[] GetFiles(string path) {
    // Same as what the real framework does
    return this.GetFiles(path, "*");
  }

  /// <inheritdoc />
  public override string[] GetFiles(string path, string searchPattern) {
    // Same as what the real framework does
    return this.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
  }

  /// <inheritdoc />
  public override string[] GetFiles(string path,
                                    string searchPattern,
                                    SearchOption searchOption) {
    return this.GetFilesInternal_(this.imaginaryFileDataAccessor_.AllFiles,
                                 path,
                                 searchPattern,
                                 searchOption);
  }

#if FEATURE_ENUMERATION_OPTIONS
        /// <inheritdoc />
        public override string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            return this.GetFiles(path, searchPattern, EnumerationOptionsToSearchOption_(enumerationOptions));
        }
#endif

  private string[] GetFilesInternal_(
      IEnumerable<string> files,
      string path,
      string searchPattern,
      SearchOption searchOption) {
    if (path == null) {
      throw new ArgumentNullException(nameof(path));
    }

    if (path.Any(c => Path.GetInvalidPathChars().Contains(c))) {
      throw new ArgumentException("Invalid character(s) in path", nameof(path));
    }

    this.CheckSearchPattern(searchPattern);
    if (searchPattern.Equals(string.Empty,
                             StringComparison.OrdinalIgnoreCase)) {
      searchPattern = "*";
    }

    path = path.TrimSlashes();
    path = path.NormalizeSlashes();
    path = this.imaginaryFileDataAccessor_.Path.GetFullPath(path);

    if (!this.Exists(path)) {
      throw CommonExceptions.CouldNotFindPartOfPath(path);
    }

    if (!path.EndsWith(Path.DirectorySeparatorChar.ToString())) {
      path += Path.DirectorySeparatorChar;
    }

    var isUnix = ImaginaryUnixSupport.IsUnixPlatform();

    var allDirectoriesPattern = isUnix
        ? @"([^<>:""/|?*]*/)*"
        : @"([^<>:""/\\|?*]*\\)*";

    var searchEndInStarDot = searchPattern.EndsWith(@"*.");

    string fileNamePattern;
    string pathPatternNoExtension = string.Empty;
    string pathPatternEndsInDot = string.Empty;
    string pathPatternSpecial = null;

    if (searchPattern == "*") {
      fileNamePattern = isUnix ? @"[^/]*?/?" : @"[^\\]*?\\?";
    } else {
      fileNamePattern = Regex.Escape(searchPattern)
                             .Replace(@"\*",
                                      isUnix
                                          ? @"[^<>:""/|?*]*?"
                                          : @"[^<>:""/\\|?*]*?")
                             .Replace(@"\?",
                                      isUnix
                                          ? @"[^<>:""/|?*]?"
                                          : @"[^<>:""/\\|?*]?");

      var extension = Path.GetExtension(searchPattern);
      bool hasExtensionLengthOfThree = extension != null &&
                                       extension.Length == 4 &&
                                       !extension.Contains("*") &&
                                       !extension.Contains("?");
      if (hasExtensionLengthOfThree) {
        var fileNamePatternSpecial =
            string.Format(CultureInfo.InvariantCulture,
                          "{0}[^.]",
                          fileNamePattern);
        pathPatternSpecial = string.Format(
            CultureInfo.InvariantCulture,
            isUnix ? @"(?i:^{0}{1}{2}(?:/?)$)" : @"(?i:^{0}{1}{2}(?:\\?)$)",
            Regex.Escape(path),
            searchOption == SearchOption.AllDirectories
                ? allDirectoriesPattern
                : string.Empty,
            fileNamePatternSpecial);
      }
    }

    var pathPattern = string.Format(
        CultureInfo.InvariantCulture,
        isUnix ? @"(?i:^{0}{1}{2}(?:/?)$)" : @"(?i:^{0}{1}{2}(?:\\?)$)",
        Regex.Escape(path),
        searchOption == SearchOption.AllDirectories
            ? allDirectoriesPattern
            : string.Empty,
        fileNamePattern);

    if (searchEndInStarDot) {
      pathPatternNoExtension
          = ReplaceLastOccurrence_(pathPattern, @"]*?\.", @"\.]*?[.]*");
      pathPatternEndsInDot
          = ReplaceLastOccurrence_(pathPattern, @"]*?\.", @"]*?[.]{1,}");
    }

    return files.Where(p =>
                           !searchEndInStarDot
                               ? (Regex.IsMatch(p, pathPattern) ||
                                  (pathPatternSpecial != null &&
                                   Regex.IsMatch(p, pathPatternSpecial)))
                               : (Regex.IsMatch(p, pathPatternNoExtension) ||
                                  Regex.IsMatch(p, pathPatternEndsInDot))
                )
                .ToArray();
  }

  /// <inheritdoc />
  public override string[] GetFileSystemEntries(string path) {
    return this.GetFileSystemEntries(path, "*");
  }

  /// <inheritdoc />
  public override string[] GetFileSystemEntries(
      string path,
      string searchPattern) {
    var dirs = this.GetDirectories(path, searchPattern);
    var files = this.GetFiles(path, searchPattern);

    return dirs.Union(files).ToArray();
  }

  /// <inheritdoc />
  public override string[] GetFileSystemEntries(
      string path,
      string searchPattern,
      SearchOption searchOption) {
    var dirs = this.GetDirectories(path, searchPattern, searchOption);
    var files = this.GetFiles(path, searchPattern, searchOption);

    return dirs.Union(files).ToArray();
  }

#if FEATURE_ENUMERATION_OPTIONS
        /// <inheritdoc />
        public override string[] GetFileSystemEntries(string path, string searchPattern,
            EnumerationOptions enumerationOptions)
        {
            return this.GetFileSystemEntries(path, "*", EnumerationOptionsToSearchOption_(enumerationOptions));
        }
#endif

  /// <inheritdoc />
  public override DateTime GetLastAccessTime(string path) {
    return this.imaginaryFileDataAccessor_.File.GetLastAccessTime(path);
  }

  /// <inheritdoc />
  public override DateTime GetLastAccessTimeUtc(string path) {
    return this.imaginaryFileDataAccessor_.File.GetLastAccessTimeUtc(path);
  }

  /// <inheritdoc />
  public override DateTime GetLastWriteTime(string path) {
    return this.imaginaryFileDataAccessor_.File.GetLastWriteTime(path);
  }

  /// <inheritdoc />
  public override DateTime GetLastWriteTimeUtc(string path) {
    return this.imaginaryFileDataAccessor_.File.GetLastWriteTimeUtc(path);
  }

  /// <inheritdoc />
  public override string[] GetLogicalDrives() {
    return this.imaginaryFileDataAccessor_
               .AllDirectories
               .Select(d => new ImaginaryDirectoryInfo(
                           this.imaginaryFileDataAccessor_,
                           d).Root.FullName)
               .Select(r => this.imaginaryFileDataAccessor_.StringOperations
                                .ToUpper(r))
               .Distinct()
               .ToArray();
  }

  /// <inheritdoc />
  public override IDirectoryInfo GetParent(string path) {
    if (path == null) {
      throw new ArgumentNullException(nameof(path));
    }

    if (path.Length == 0) {
      throw new ArgumentException(
          StringResources.Manager.GetString(
              "PATH_CANNOT_BE_THE_EMPTY_STRING_OR_ALL_WHITESPACE"),
          "path");
    }

    if (this.imaginaryFileDataAccessor_.ImaginaryPathVerifier.HasIllegalCharacters(
            path,
            false)) {
      throw new ArgumentException("Path contains invalid path characters.",
                                  "path");
    }

    var absolutePath = this.imaginaryFileDataAccessor_.Path.GetFullPath(path);
    var sepAsString = this.imaginaryFileDataAccessor_.Path
                          .DirectorySeparatorChar.ToString();
    var lastIndex = 0;

    if (absolutePath != sepAsString) {
      var startIndex
          = this.imaginaryFileDataAccessor_.StringOperations.EndsWith(
              absolutePath,
              sepAsString)
              ? absolutePath.Length - 1
              : absolutePath.Length;
      lastIndex = absolutePath.LastIndexOf(
          this.imaginaryFileDataAccessor_.Path.DirectorySeparatorChar,
          startIndex - 1);

      if (lastIndex < 0) {
        return null;
      }
    }

    var parentPath = absolutePath.Substring(0, lastIndex);

    if (string.IsNullOrEmpty(parentPath)) {
      // On the Unix platform, the parent of a path consisting of a slash followed by
      // non-slashes is the root, '/'.
      if (ImaginaryUnixSupport.IsUnixPlatform()) {
        absolutePath = absolutePath.TrimSlashes();

        if (absolutePath.Length > 1 &&
            absolutePath.LastIndexOf(this.imaginaryFileDataAccessor_.Path
                                         .DirectorySeparatorChar) ==
            0) {
          return new ImaginaryDirectoryInfo(this.imaginaryFileDataAccessor_,
                                       this.imaginaryFileDataAccessor_.Path
                                           .DirectorySeparatorChar.ToString());
        }
      }

      return null;
    }

    return new ImaginaryDirectoryInfo(this.imaginaryFileDataAccessor_, parentPath);
  }

  /// <inheritdoc />
  public override void Move(string sourceDirName, string destDirName) {
    var fullSourcePath = this.imaginaryFileDataAccessor_.Path
                             .GetFullPath(sourceDirName)
                             .TrimSlashes();
    var fullDestPath = this.imaginaryFileDataAccessor_.Path
                           .GetFullPath(destDirName)
                           .TrimSlashes();

    if (string.Equals(fullSourcePath, fullDestPath, StringComparison.Ordinal)) {
      throw new IOException("Source and destination path must be different.");
    }

    //if we're moving a file, not a directory, call the appropriate file moving function.
    var fileData = this.imaginaryFileDataAccessor_.GetFile(fullSourcePath);
    if (fileData?.Attributes.HasFlag(FileAttributes.Directory) == false) {
      this.imaginaryFileDataAccessor_.File.Move(fullSourcePath, fullDestPath);
      return;
    }

    var sourceRoot
        = this.imaginaryFileDataAccessor_.Path.GetPathRoot(fullSourcePath);
    var destinationRoot
        = this.imaginaryFileDataAccessor_.Path.GetPathRoot(fullDestPath);

    if (!this.imaginaryFileDataAccessor_.StringOperations.Equals(
            sourceRoot,
            destinationRoot)) {
      throw new IOException(
          "Source and destination path must have identical roots. Move will not work across volumes.");
    }

    if (!this.imaginaryFileDataAccessor_.Directory.Exists(fullSourcePath)) {
      throw CommonExceptions.CouldNotFindPartOfPath(sourceDirName);
    }

    if (!this.imaginaryFileDataAccessor_.Directory.GetParent(fullDestPath)
             .Exists) {
      throw CommonExceptions.CouldNotFindPartOfPath(destDirName);
    }

    if (this.imaginaryFileDataAccessor_.Directory.Exists(fullDestPath) ||
        this.imaginaryFileDataAccessor_.File.Exists(fullDestPath)) {
      // In Windows, file/dir names are case sensetive, C:\\temp\\src and C:\\temp\\SRC and treated different
      if (ImaginaryUnixSupport.IsUnixPlatform() ||
          !string.Equals(fullSourcePath,
                         fullDestPath,
                         StringComparison.OrdinalIgnoreCase)) {
        throw CommonExceptions.CannotCreateBecauseSameNameAlreadyExists(
            fullDestPath);
      }
    }

    this.imaginaryFileDataAccessor_.MoveDirectory(fullSourcePath, fullDestPath);
  }

#if FEATURE_CREATE_SYMBOLIC_LINK
        /// <inheritdoc />
        public override IFileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget)
        {
            var initialContainer = this.imaginaryFileDataAccessor_.GetFile(linkPath);
            if (initialContainer.LinkTarget != null)
            {
                var nextLocation = initialContainer.LinkTarget;
                var nextContainer = this.imaginaryFileDataAccessor_.GetFile(nextLocation);

                if (returnFinalTarget)
                {
                    // The maximum number of symbolic links that are followed:
                    // https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget?view=net-6.0#remarks
                    int maxResolveLinks = XFS.IsWindowsPlatform() ? 63 : 40;
                    for (int i = 1; i < maxResolveLinks; i++)
                    {
                        if (nextContainer.LinkTarget == null)
                        {
                            break;
                        }
                        nextLocation = nextContainer.LinkTarget;
                        nextContainer = this.imaginaryFileDataAccessor_.GetFile(nextLocation);
                    }

                    if (nextContainer.LinkTarget != null)
                    {
                        throw CommonExceptions.NameCannotBeResolvedByTheSystem(linkPath);
                    }
                }

                if (nextContainer.IsDirectory)
                {
                    return new ImaginaryDirectoryInfo(this.imaginaryFileDataAccessor_, nextLocation);
                }
                else
                {
                    return new ImaginaryFileInfo(this.imaginaryFileDataAccessor_, nextLocation);
                }
            }
            throw CommonExceptions.NameCannotBeResolvedByTheSystem(linkPath);
        }

#endif

  /// <inheritdoc />
  public override void SetCreationTime(string path, DateTime creationTime) {
    this.imaginaryFileDataAccessor_.File.SetCreationTime(path, creationTime);
  }

  /// <inheritdoc />
  public override void
      SetCreationTimeUtc(string path, DateTime creationTimeUtc) {
    this.imaginaryFileDataAccessor_.File.SetCreationTimeUtc(
        path,
        creationTimeUtc);
  }

  /// <inheritdoc />
  public override void SetCurrentDirectory(string path) {
    this.currentDirectory = this.imaginaryFileDataAccessor_.Path.GetFullPath(path);
  }

  /// <inheritdoc />
  public override void SetLastAccessTime(string path, DateTime lastAccessTime) {
    this.imaginaryFileDataAccessor_.File
        .SetLastAccessTime(path, lastAccessTime);
  }

  /// <inheritdoc />
  public override void SetLastAccessTimeUtc(string path,
                                            DateTime lastAccessTimeUtc) {
    this.imaginaryFileDataAccessor_.File.SetLastAccessTimeUtc(
        path,
        lastAccessTimeUtc);
  }

  /// <inheritdoc />
  public override void SetLastWriteTime(string path, DateTime lastWriteTime) {
    this.imaginaryFileDataAccessor_.File.SetLastWriteTime(path, lastWriteTime);
  }

  /// <inheritdoc />
  public override void SetLastWriteTimeUtc(string path,
                                           DateTime lastWriteTimeUtc) {
    this.imaginaryFileDataAccessor_.File.SetLastWriteTimeUtc(
        path,
        lastWriteTimeUtc);
  }

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateDirectories(string path) {
    return this.EnumerateDirectories(path, "*");
  }

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateDirectories(
      string path,
      string searchPattern) {
    return this.EnumerateDirectories(path,
                                     searchPattern,
                                     SearchOption.TopDirectoryOnly);
  }

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateDirectories(
      string path,
      string searchPattern,
      SearchOption searchOption) {
    this.imaginaryFileDataAccessor_.ImaginaryPathVerifier.IsLegalAbsoluteOrRelative(
        path,
        "path");
    var originalPath = path;
    path = path.TrimSlashes();
    path = this.imaginaryFileDataAccessor_.Path.GetFullPath(path);
    return this.GetFilesInternal_(this.imaginaryFileDataAccessor_.AllDirectories,
                                 path,
                                 searchPattern,
                                 searchOption)
               .Where(p => !this.imaginaryFileDataAccessor_.StringOperations.Equals(
                          p,
                          path))
               .Select(p => this.FixPrefix(p, originalPath));
  }

  private string FixPrefix(string path, string originalPath) {
    var normalizedOriginalPath
        = this.imaginaryFileDataAccessor_.Path.GetFullPath(originalPath);
    var pathWithoutOriginalPath = path.Substring(normalizedOriginalPath.Length)
                                      .TrimStart(
                                          this.imaginaryFileDataAccessor_.Path
                                              .DirectorySeparatorChar);
    return this.imaginaryFileDataAccessor_.Path.Combine(
        originalPath,
        pathWithoutOriginalPath);
  }

#if FEATURE_ENUMERATION_OPTIONS
        /// <inheritdoc />
        public override IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            var searchOption =
 enumerationOptions.RecurseSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return this.EnumerateDirectories(path, searchPattern, searchOption);
        }
#endif

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateFiles(string path) {
    return this.GetFiles(path);
  }

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateFiles(
      string path,
      string searchPattern) {
    return this.GetFiles(path, searchPattern);
  }

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateFiles(
      string path,
      string searchPattern,
      SearchOption searchOption) {
    return this.GetFiles(path, searchPattern, searchOption);
  }

#if FEATURE_ENUMERATION_OPTIONS
        /// <inheritdoc />
        public override IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            var searchOption =
 enumerationOptions.RecurseSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return this.GetFiles(path, searchPattern, searchOption);
        }
#endif

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateFileSystemEntries(string path) {
    return this.GetFileSystemEntries(path);
  }

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateFileSystemEntries(
      string path,
      string searchPattern) {
    return this.GetFileSystemEntries(path, searchPattern);
  }

  /// <inheritdoc />
  public override IEnumerable<string> EnumerateFileSystemEntries(
      string path,
      string searchPattern,
      SearchOption searchOption) {
    return this.GetFileSystemEntries(path, searchPattern, searchOption);
  }

#if FEATURE_ENUMERATION_OPTIONS
        /// <inheritdoc />
        public override IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            var searchOption =
 enumerationOptions.RecurseSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var fileSystemEntries =
 new List<string>(this.GetFiles(path, searchPattern, searchOption));
            fileSystemEntries.AddRange(this.GetDirectories(path, searchPattern, searchOption));
            return fileSystemEntries;
        }
#endif

  private string EnsureAbsolutePath(string path) {
    return Path.IsPathRooted(path)
        ? path
        : Path.Combine(this.GetCurrentDirectory(), path);
  }

  private void CheckSearchPattern(string searchPattern) {
    if (searchPattern == null) {
      throw new ArgumentNullException(nameof(searchPattern));
    }

    const string TWO_DOTS = "..";
    Func<ArgumentException> createException = ()
        => new ArgumentException(
            @"Search pattern cannot contain "".."" to move up directories and can be contained only internally in file/directory names, as in ""a..b"".",
            searchPattern);

    if (this.imaginaryFileDataAccessor_.StringOperations.EndsWith(
            searchPattern,
            TWO_DOTS)) {
      throw createException();
    }

    var position
        = this.imaginaryFileDataAccessor_.StringOperations.IndexOf(
            searchPattern,
            TWO_DOTS);

    if (position >= 0) {
      var characterAfterTwoDots = searchPattern[position + 2];

      if (characterAfterTwoDots == Path.DirectorySeparatorChar ||
          characterAfterTwoDots == Path.AltDirectorySeparatorChar) {
        throw createException();
      }
    }

    var invalidPathChars = Path.GetInvalidPathChars();
    if (searchPattern.IndexOfAny(invalidPathChars) > -1) {
      throw CommonExceptions.IllegalCharactersInPath(nameof(searchPattern));
    }
  }

  private static string ReplaceLastOccurrence_(string source,
                                               string find,
                                               string replace) {
    if (source == null) {
      return source;
    }

    var place = source.LastIndexOf(find);

    if (place == -1) {
      return source;
    }

    var result = source.Remove(place, find.Length).Insert(place, replace);
    return result;
  }

#if FEATURE_ENUMERATION_OPTIONS
        private static SearchOption EnumerationOptionsToSearchOption_(EnumerationOptions enumerationOptions)
        {
            static Exception CreateExceptionForUnsupportedProperty(string propertyName)
            {
                return new NotSupportedException(
                    $"Changing EnumerationOptions.{propertyName} is not yet implemented for the mock file system."
                );
            }

            if (enumerationOptions.AttributesToSkip != (FileAttributes.System | FileAttributes.Hidden))
            {
                throw CreateExceptionForUnsupportedProperty("AttributesToSkip");
            }
            if (!enumerationOptions.IgnoreInaccessible)
            {
                throw CreateExceptionForUnsupportedProperty("IgnoreInaccessible");
            }
            if (enumerationOptions.MatchCasing != MatchCasing.PlatformDefault)
            {
                throw CreateExceptionForUnsupportedProperty("MatchCasing");
            }
            if (enumerationOptions.MatchType != MatchType.Simple)
            {
                throw CreateExceptionForUnsupportedProperty("MatchType");
            }
            if (enumerationOptions.ReturnSpecialDirectories)
            {
                throw CreateExceptionForUnsupportedProperty("ReturnSpecialDirectories");
            }

            return enumerationOptions.RecurseSubdirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;
        }
#endif
}