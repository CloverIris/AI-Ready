package main

import (
	"fmt"
	"io/fs"
	"os"
	"path/filepath"
	"strings"
)

// FileManager handles file operations
type FileManager struct {
	basePath string // Base path restriction for security
}

// FileInfo represents file or directory information
type FileInfo struct {
	Name    string `json:"name"`
	Path    string `json:"path"`
	Size    int64  `json:"size"`
	Mode    string `json:"mode"`
	ModTime int64  `json:"mod_time"`
	IsDir   bool   `json:"is_dir"`
	Items   int    `json:"items,omitempty"` // Number of items if directory
}

// NewFileManager creates a new file manager
func NewFileManager() *FileManager {
	return &FileManager{
		basePath: "/", // Allow access to entire filesystem by default
	}
}

// List lists files in a directory
func (fm *FileManager) List(path string) (*FileList, error) {
	// Security: prevent directory traversal
	cleanPath := filepath.Clean(path)
	if strings.Contains(cleanPath, "..") {
		return nil, fmt.Errorf("invalid path")
	}

	// If path is relative, make it absolute
	if !filepath.IsAbs(cleanPath) {
		cleanPath = filepath.Join(fm.basePath, cleanPath)
	}

	// Get file info
	info, err := os.Stat(cleanPath)
	if err != nil {
		return nil, fmt.Errorf("cannot access path: %w", err)
	}

	// If it's a file, return file info
	if !info.IsDir() {
		return &FileList{
			Path:  cleanPath,
			IsDir: false,
			Items: []FileInfo{fm.toFileInfo(info, cleanPath)},
		}, nil
	}

	// Read directory
	entries, err := os.ReadDir(cleanPath)
	if err != nil {
		return nil, fmt.Errorf("cannot read directory: %w", err)
	}

	items := make([]FileInfo, 0, len(entries))
	for _, entry := range entries {
		info, err := entry.Info()
		if err != nil {
			continue
		}
		
		fullPath := filepath.Join(cleanPath, entry.Name())
		fileInfo := fm.toFileInfo(info, fullPath)
		
		// Count items if directory
		if entry.IsDir() {
			if subEntries, err := os.ReadDir(fullPath); err == nil {
				fileInfo.Items = len(subEntries)
			}
		}
		
		items = append(items, fileInfo)
	}

	return &FileList{
		Path:  cleanPath,
		IsDir: true,
		Items: items,
	}, nil
}

// toFileInfo converts fs.FileInfo to our FileInfo
func (fm *FileManager) toFileInfo(info fs.FileInfo, fullPath string) FileInfo {
	return FileInfo{
		Name:    info.Name(),
		Path:    fullPath,
		Size:    info.Size(),
		Mode:    info.Mode().String(),
		ModTime: info.ModTime().Unix(),
		IsDir:   info.IsDir(),
	}
}

// FileList represents a directory listing
type FileList struct {
	Path  string    `json:"path"`
	IsDir bool      `json:"is_dir"`
	Items []FileInfo `json:"items"`
}

// Read reads a file
func (fm *FileManager) Read(path string) ([]byte, error) {
	cleanPath := filepath.Clean(path)
	if strings.Contains(cleanPath, "..") {
		return nil, fmt.Errorf("invalid path")
	}

	return os.ReadFile(cleanPath)
}

// Write writes a file
func (fm *FileManager) Write(path string, data []byte) error {
	cleanPath := filepath.Clean(path)
	if strings.Contains(cleanPath, "..") {
		return fmt.Errorf("invalid path")
	}

	// Ensure parent directory exists
	parent := filepath.Dir(cleanPath)
	if err := os.MkdirAll(parent, 0755); err != nil {
		return fmt.Errorf("cannot create directory: %w", err)
	}

	return os.WriteFile(cleanPath, data, 0644)
}

// Delete deletes a file or directory
func (fm *FileManager) Delete(path string) error {
	cleanPath := filepath.Clean(path)
	if strings.Contains(cleanPath, "..") {
		return fmt.Errorf("invalid path")
	}

	return os.RemoveAll(cleanPath)
}

// Mkdir creates a directory
func (fm *FileManager) Mkdir(path string) error {
	cleanPath := filepath.Clean(path)
	if strings.Contains(cleanPath, "..") {
		return fmt.Errorf("invalid path")
	}

	return os.MkdirAll(cleanPath, 0755)
}
