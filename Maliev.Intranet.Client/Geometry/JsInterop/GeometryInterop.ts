// Maliev.Intranet.Client/Geometry/JsInterop/GeometryInterop.ts
import { BabylonThumbnailGenerator, ThumbnailSet, ThumbnailOptions } from "../BabylonThumbnailGenerator";

/**
 * JS interop wrapper for Blazor to invoke client-side thumbnail generation.
 * Exposed as global function `MalievGeometry.generateThumbnails`.
 */
export class GeometryInterop {
  private static generator: BabylonThumbnailGenerator | null = null;

  /**
   * Generates all 8 thumbnail views for a file URL.
   * @param fileUrl - Signed GCS download URL
   * @param options - Generation options (timeout, quality, progress callback)
   * @returns Promise resolving to ThumbnailSet
   */
  public static async generateThumbnails(
    fileUrl: string,
    options: ThumbnailOptions = {}
  ): Promise<ThumbnailSet> {
    if (!this.generator) {
      this.generator = new BabylonThumbnailGenerator();
    }
    return await this.generator.generateAllViews(fileUrl, options);
  }

  /**
   * Checks if WebGL 2 is available in the current browser.
   */
  public static isWebGL2Available(): boolean {
    try {
      const canvas = document.createElement("canvas");
      return !!canvas.getContext("webgl2");
    } catch {
      return false;
    }
  }
}

// Expose to global window for Blazor JSInterop
declare global {
  interface Window {
    MalievGeometry: {
      generateThumbnails: (fileUrl: string, options: ThumbnailOptions) => Promise<ThumbnailSet>;
      isWebGL2Available: () => boolean;
    };
  }
}

if (typeof window !== "undefined") {
  window.MalievGeometry = {
    generateThumbnails: (fileUrl, options) => GeometryInterop.generateThumbnails(fileUrl, options),
    isWebGL2Available: () => GeometryInterop.isWebGL2Available()
  };
}