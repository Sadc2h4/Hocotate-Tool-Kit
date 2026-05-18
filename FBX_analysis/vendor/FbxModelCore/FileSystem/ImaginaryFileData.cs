using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text;

namespace System.IO.Abstractions.TestingHelpers;

/// <summary>
/// The class represents the associated data of a file.
/// </summary>
#if FEATURE_SERIALIZABLE
[Serializable]
#endif
public class ImaginaryFileData {
  /// <summary>
  /// The default encoding.
  /// </summary>
  public static readonly Encoding DefaultEncoding
      = new UTF8Encoding(false, true);

  /// <summary>
  /// The null object. It represents the data of a non-existing file or directory.
  /// </summary>
  internal static readonly ImaginaryFileData NullObject
      = new ImaginaryFileData(string.Empty) {
          LastWriteTime
              = new DateTime(1601, 01, 01, 00, 00, 00, DateTimeKind.Utc),
          LastAccessTime
              = new DateTime(1601, 01, 01, 00, 00, 00, DateTimeKind.Utc),
          CreationTime
              = new DateTime(1601, 01, 01, 00, 00, 00, DateTimeKind.Utc),
          Attributes = (FileAttributes) (-1),
      };

  /// <summary>
  /// Gets the default date time offset.
  /// E.g. for not existing files.
  /// </summary>
  public static readonly DateTimeOffset DefaultDateTimeOffset
      = new DateTime(1601, 01, 01, 00, 00, 00, DateTimeKind.Utc);

  /// <summary>
  /// The access control of the <see cref="ImaginaryFileData"/>.
  /// </summary>
#if FEATURE_SERIALIZABLE
    [NonSerialized]
#endif
  private FileSecurity accessControl;

  /// <summary>
  /// Gets a value indicating whether the <see cref="ImaginaryFileData"/> is a directory or not.
  /// </summary>
  public bool IsDirectory {
    get { return Attributes.HasFlag(FileAttributes.Directory); }
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryFileData"/> class with an empty content.
  /// </summary>
  private ImaginaryFileData() {
    var now = DateTime.UtcNow;
    LastWriteTime = now;
    LastAccessTime = now;
    CreationTime = now;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryFileData"/> class with the content of <paramref name="textContents"/> using the encoding of <see cref="DefaultEncoding"/>.
  /// </summary>
  /// <param name="textContents">The textual content encoded into bytes with <see cref="DefaultEncoding"/>.</param>
  public ImaginaryFileData(string textContents)
      : this(DefaultEncoding.GetBytes(textContents)) { }

  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryFileData"/> class with the content of <paramref name="textContents"/> using the encoding of <paramref name="encoding"/>.
  /// </summary>
  /// <param name="textContents">The textual content.</param>
  /// <param name="encoding">The specific encoding used the encode the text.</param>
  /// <remarks>The constructor respect the BOM of <paramref name="encoding"/>.</remarks>
  public ImaginaryFileData(string textContents, Encoding encoding)
      : this() {
    Contents = encoding.GetPreamble()
                       .Concat(encoding.GetBytes(textContents))
                       .ToArray();
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryFileData"/> class with the content of <paramref name="contents"/>.
  /// </summary>
  /// <param name="contents">The actual content.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="contents"/> is <see langword="null" />.</exception>
  public ImaginaryFileData(byte[] contents)
      : this() {
    Contents = contents ?? throw new ArgumentNullException(nameof(contents));
  }


  /// <summary>
  /// Initializes a new instance of the <see cref="ImaginaryFileData"/> class by copying the given <see cref="ImaginaryFileData"/>.
  /// </summary>
  /// <param name="template">The template instance.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> is <see langword="null" />.</exception>
  public ImaginaryFileData(ImaginaryFileData template) {
    if (template == null) {
      throw new ArgumentNullException(nameof(template));
    }

    accessControl = template.accessControl;
    Attributes = template.Attributes;
    Contents = template.Contents.ToArray();
    CreationTime = template.CreationTime;
    LastAccessTime = template.LastAccessTime;
    LastWriteTime = template.LastWriteTime;
#if FEATURE_FILE_SYSTEM_INFO_LINK_TARGET
        LinkTarget = template.LinkTarget;
#endif
  }

  /// <summary>
  /// Gets or sets the byte contents of the <see cref="ImaginaryFileData"/>.
  /// </summary>
  public byte[] Contents { get; set; }

  /// <summary>
  /// Gets or sets the file version info of the <see cref="ImaginaryFileData"/>
  /// </summary>
  public IFileVersionInfo FileVersionInfo { get; set; }

  /// <summary>
  /// Gets or sets the string contents of the <see cref="ImaginaryFileData"/>.
  /// </summary>
  /// <remarks>
  /// The setter uses the <see cref="DefaultEncoding"/> using this can scramble the actual contents.
  /// </remarks>
  public string TextContents {
    get { return ImaginaryFile.ReadAllBytes(Contents, DefaultEncoding); }
    set { Contents = DefaultEncoding.GetBytes(value); }
  }

  /// <summary>
  /// Gets or sets the date and time the <see cref="ImaginaryFileData"/> was created.
  /// </summary>
  public DateTimeOffset CreationTime {
    get { return creationTime; }
    set { creationTime = value.ToUniversalTime(); }
  }

  private DateTimeOffset creationTime;

  /// <summary>
  /// Gets or sets the date and time of the <see cref="ImaginaryFileData"/> was last accessed to.
  /// </summary>
  public DateTimeOffset LastAccessTime {
    get { return lastAccessTime; }
    set { lastAccessTime = value.ToUniversalTime(); }
  }

  private DateTimeOffset lastAccessTime;

  /// <summary>
  /// Gets or sets the date and time of the <see cref="ImaginaryFileData"/> was last written to.
  /// </summary>
  public DateTimeOffset LastWriteTime {
    get { return lastWriteTime; }
    set { lastWriteTime = value.ToUniversalTime(); }
  }

  private DateTimeOffset lastWriteTime;

#if FEATURE_FILE_SYSTEM_INFO_LINK_TARGET
    /// <summary>
    /// Gets or sets the link target of the <see cref="ImaginaryFileData"/>.
    /// </summary>
    public string LinkTarget { get; set; }
#endif

  /// <summary>
  /// Casts a string into <see cref="ImaginaryFileData"/>.
  /// </summary>
  /// <param name="s">The path of the <see cref="ImaginaryFileData"/> to be created.</param>
  public static implicit operator ImaginaryFileData(string s) {
    return new ImaginaryFileData(s);
  }

  /// <summary>
  /// Gets or sets the specified <see cref="FileAttributes"/> of the <see cref="ImaginaryFileData"/>.
  /// </summary>
  public FileAttributes Attributes { get; set; } = FileAttributes.Normal;

  /// <summary>
  /// Gets or sets <see cref="FileSecurity"/> of the <see cref="ImaginaryFileData"/>.
  /// </summary>
  [SupportedOSPlatform("windows")]
  public FileSecurity AccessControl {
    get {
      // FileSecurity's constructor will throw PlatformNotSupportedException on non-Windows platform, so we initialize it in lazy way.
      // This let's us use this class as long as we don't use AccessControl property.
      return accessControl ?? (accessControl = new FileSecurity());
    }
    set { accessControl = value; }
  }

  /// <summary>
  /// Gets or sets the File sharing mode for this file, this allows you to lock a file for reading or writing.
  /// </summary>
  public FileShare AllowedFileShare { get; set; }
    = FileShare.ReadWrite | FileShare.Delete;

#if FEATURE_UNIX_FILE_MODE
        /// <summary>
        /// Gets or sets the Unix file mode (permissions) for this file.
        /// This allows you to configure the read, write and execute access for user, group and other.
        /// </summary>
        public UnixFileMode UnixMode { get; set; } =
 UnixFileMode.UserRead | UnixFileMode.GroupRead |
                                                     UnixFileMode.OtherRead | UnixFileMode.UserWrite;
#endif

  /// <summary>
  /// Checks whether the file is accessible for this type of FileAccess. 
  /// ImaginaryFileData can be configured to have FileShare.None, which indicates it is locked by a 'different process'.
  /// 
  /// If the file is 'locked by a different process', an IOException will be thrown.
  /// If the file is read-only and is accessed for writing, an UnauthorizedAccessException will be thrown.
  /// </summary>
  /// <param name="path">The path is used in the exception message to match the message in real life situations</param>
  /// <param name="access">The access type to check</param>
  internal void CheckFileAccess(string path, FileAccess access) {
    if (!AllowedFileShare.HasFlag((FileShare) access)) {
      throw CommonExceptions.ProcessCannotAccessFileInUse(path);
    }

    if (Attributes.HasFlag(FileAttributes.ReadOnly) &&
        access.HasFlag(FileAccess.Write)) {
      throw CommonExceptions.AccessDenied(path);
    }
  }

  internal virtual ImaginaryFileData Clone() {
    return new ImaginaryFileData(this);
  }
}