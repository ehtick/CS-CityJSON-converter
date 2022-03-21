﻿namespace CSCJConverter.tileset;

public abstract class AbstractTileset : ITileset
{
    private decimal? _maxX;
    private decimal? _maxY;
    private decimal? _maxZ;
    private decimal? _minX;
    private decimal? _minY;
    private decimal? _minZ;

    private readonly string _version;
    private readonly string _gltfUpAxis;

    private readonly decimal _tilesetGeometricError;
    private readonly decimal _rootGeometricError;
    protected decimal _tileGeometricError;

    public enum RefineMethods
    {
        ADD,
        REPLACE
    }
    private string _rootRefineMethod;

    /// <summary>
    /// Constructor for the base Tileset class used to build a tileset.
    /// </summary>
    /// <param name="tilesetGeometricError">
    ///     The error, in meters, introduced if this tileset is not rendered. At runtime, the geometric error is used
    ///     to compute screen space error (SSE), i.e., the error measured in pixels. Default: 260
    /// </param>
    /// <param name="rootGeometricError">
    ///     Nonnegative number that defines the error, in meters. Is used at runtime to determine the SSE at which the
    ///     root tile's children are rendered. Default: 4.5398975185470771
    /// </param>
    /// <param name="tileGeometricError">
    ///     The error, in meters, introduced if this tile is rendered and its children are not. At runtime,
    ///     the geometric error is used to compute screen space error (SSE), i.e., the error measured in pixels.
    ///     Default: 2.3232
    /// </param>
    /// <param name="version">Default: 1.0</param>
    /// <param name="gltfUpAxis">Default: z</param>
    /// <param name="refineMethod">Default: REPLACE</param>
    /// <param name="structureType">Default: GRID</param>
    public AbstractTileset(decimal tilesetGeometricError = 260,
        decimal rootGeometricError = 4.5398975185470771m,
        decimal tileGeometricError = 2.3232m,
        string version = "1.0",
        string gltfUpAxis = "z",
        RefineMethods refineMethod = RefineMethods.REPLACE)
    {
        this._version = version;
        this._gltfUpAxis = gltfUpAxis;
        this._rootRefineMethod = refineMethod == RefineMethods.ADD ? "ADD" : "REPLACE";
        this._tilesetGeometricError = tilesetGeometricError;
        this._rootGeometricError = rootGeometricError;
        this._tileGeometricError = tileGeometricError;
    }

    /// <summary>
    /// Base generator for a tileset. Adds an IEnumerable of children onto a root
    /// tile. Does not support content on the root tile!
    /// </summary>
    /// <param name="children">
    ///     An array of objects that define child tiles. Each child tile content is fully enclosed by its parent tile's
    ///     bounding volume and, generally, has a geometricError less than its parent tile's geometricError. For leaf
    ///     tiles, the length of this array is zero, and children may not be defined.
    /// </param>
    /// <returns>A TilesetModel which can be serialized to a tileset.json file.</returns>
    protected TilesetModel GenerateTileSet(IList<Tile> children)
    {
        Asset asset = new Asset()
        {
            version = this._version,
            gltfUpAxis = this._gltfUpAxis

        };
        
        decimal rootCenterX = this.CalculateCenter((decimal)_maxX, (decimal)_minX);
        decimal rootCenterY = this.CalculateCenter((decimal)_maxY, (decimal)_minY);
        decimal rootCenterZ = this.CalculateCenter((decimal)_maxZ, (decimal)_minZ);
        decimal rootHalfX = this.CalculateHalfLength((decimal)_maxX, (decimal)_minX);
        decimal rootHalfY = this.CalculateHalfLength((decimal)_maxY, (decimal)_minY);
        decimal rootHalfZ = this.CalculateHalfLength((decimal)_maxZ, (decimal)_minZ);
        BoxVolume rootBox = new BoxVolume()
        {
            box = new decimal[12]
            {
                rootCenterX, rootCenterY, rootCenterZ,
                rootHalfX, 0, 0,
                0, rootHalfY, 0,
                0, 0, rootHalfZ
            }
        };
        
        Root root = new Root()
        {
            boundingVolume = rootBox,
            transform = new int[16]
            {
                1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1
            },
            children = children,
            geometricError = this._rootGeometricError,
            refine = this._rootRefineMethod
        };
        
        return new TilesetModel()
        {
            asset = asset,
            root = root,
            geometricError = _tilesetGeometricError
        };
    }

    public abstract void AddTile(double[] geographicalExtent, string uri);

    /// <summary>
    /// Update (if required) the max / min x coords of our tileset.
    /// </summary>
    /// <param name="tileMaxX">Tile max x.</param>
    /// <param name="tileMinX">Tile min x.</param>
    protected void _updateMinMaxX(decimal tileMaxX, decimal tileMinX)
    {
        if (this._maxX == null) this._maxX = tileMaxX;
        else if (tileMaxX > this._maxX) this._maxX = tileMaxX;
                
        if (this._minX == null) this._minX = tileMinX;
        else if (tileMinX < this._minX) this._minX = tileMinX;
    }
    
    /// <summary>
    /// Update (if required) the max / min y coords of our tileset.
    /// </summary>
    /// <param name="tileMaxY">Tile max y.</param>
    /// <param name="tileMinY">Tile min y.</param>
    protected void _updateMinMaxY(decimal tileMaxY, decimal tileMinY)
    {
        if (this._maxY == null) this._maxY = tileMaxY;
        else if (tileMaxY > this._maxY) this._maxY = tileMaxY;
        if (this._minY == null) this._minY = tileMinY;
        else if (tileMinY < this._minY) this._minY = tileMinY;
        
    }
    
    /// <summary>
    /// Update (if required) the max / min z coords of our tileset.
    /// </summary>
    /// <param name="tileMaxZ">Tile max z.</param>
    /// <param name="tileMinZ">Tile min z.</param>
    protected void _updateMinMaxZ(decimal tileMaxZ, decimal tileMinZ)
    {
        if (this._maxZ == null) this._maxZ = tileMaxZ;
        else if (tileMaxZ > this._maxZ) this._maxZ = tileMaxZ;
        if (this._minZ == null) this._minZ = tileMinZ;
        else if (tileMinZ < this._minZ) this._minZ = tileMinZ;
    }

    /// <summary>
    /// Calculate the center based on two coords.
    /// </summary>
    /// <param name="maxCoordPoint">The max coord.</param>
    /// <param name="minCoordPoint">The min coord.</param>
    /// <returns>The center of both coords as decimal.</returns>
    protected decimal CalculateCenter(decimal maxCoordPoint, decimal minCoordPoint)
    {
        return this.CalculateHalfLength(maxCoordPoint, minCoordPoint) + minCoordPoint;
    }

    /// <summary>
    /// Calculate a half value of two coords.
    /// </summary>
    /// <param name="maxCoordPoint">The max coord.</param>
    /// <param name="minCoordPoint">The min coord.</param>
    /// <returns>The half value of two coords, as decimal.</returns>
    protected decimal CalculateHalfLength(decimal maxCoordPoint, decimal minCoordPoint)
    {
        return (maxCoordPoint - minCoordPoint) / 2;
    }
    
    
}